using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Cqrs;
using Lykke.Messages.Email.MessageData;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.EmailSender;
using Lykke.Service.Kyc.Abstractions.Domain.Documents;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.PushNotifications.Contract.Enums;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.Domain.Events;
using Lykke.Service.Tier.Domain.Services;
using IEmailSender = Lykke.Messages.Email.IEmailSender;

namespace Lykke.Service.Tier.Workflow.Sagas
{
    public class TierUpgradeRequestSaga
    {
        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycDocumentsServiceV2 _kycDocumentsService;
        private readonly ITiersService _tiersService;
        private readonly IEmailSender _emailSender;
        private readonly ITemplateFormatter _templateFormatter;
        private readonly IMapper _mapper;

        public TierUpgradeRequestSaga(
            ITierUpgradeService tierUpgradeService,
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            IKycDocumentsServiceV2 kycDocumentsService,
            ITiersService tiersService,
            IEmailSender emailSender,
            ITemplateFormatter templateFormatter,
            IMapper mapper
        )
        {
            _tierUpgradeService = tierUpgradeService;
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _kycDocumentsService = kycDocumentsService;
            _tiersService = tiersService;
            _emailSender = emailSender;
            _templateFormatter = templateFormatter;
            _mapper = mapper;
        }

        public Task Handle(TierUpgradeRequestChangedEvent evt, ICommandSender commandSender)
        {
            return Task.WhenAll(
                _tierUpgradeService.UpdateCountsAsync(evt.ClientId, evt.Tier, evt.OldStatus, evt.NewStatus),
                SendNotificationAsync(evt, commandSender)
            );
        }

        private async Task SendNotificationAsync(TierUpgradeRequestChangedEvent evt, ICommandSender commandSender)
        {
            var clientAccTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);
            var personalDataTask = _personalDataService.GetAsync(evt.ClientId);
            var pushSettingsTask = _clientAccountClient.ClientSettings.GetPushNotificationAsync(evt.ClientId);

            await Task.WhenAll(clientAccTask, personalDataTask, pushSettingsTask);

            var clientAcc = clientAccTask.Result;
            var personalData = personalDataTask.Result;
            var pushSettings = pushSettingsTask.Result;
            bool pushEnabled = pushSettings.Enabled && !string.IsNullOrEmpty(clientAcc.NotificationsId);

            Task<EmailMessage> emailTemplateTask = Task.FromResult<EmailMessage>(null);
            Task<EmailMessage> pushTemplateTask = Task.FromResult<EmailMessage>(null);
            string type = string.Empty;

            try
            {
                switch (evt.NewStatus)
                {
                    case KycStatus.Ok:
                        var tierInfo = await _tiersService.GetClientTierInfoAsync(evt.ClientId, clientAcc.Tier, personalData.CountryFromPOA);

                        if (tierInfo.CurrentTier.MaxLimit == 0)
                            return;

                        var sb = new StringBuilder();
                        bool noAmountTemplate = tierInfo.CurrentTier.MaxLimit > 0;

                        if (tierInfo.NextTier != null)
                        {
                            sb.AppendLine(
                                $"If you wish to increase deposit limit, just upgrade to an {tierInfo.NextTier.Tier} account.");
                            sb.AppendLine("<br>Or use Lykke Wallet mobile app (More->Profile->Upgrade)");
                        }

                        emailTemplateTask = _templateFormatter.FormatAsync(noAmountTemplate ? "TierUpgradedNoAmountTemplate" : "TierUpgradedTemplate", clientAcc.PartnerId,
                            "EN", new
                            {
                                Tier = evt.Tier.ToString(),
                                Year = DateTime.UtcNow.Year,
                                Amount = $"{tierInfo.CurrentTier.MaxLimit} {tierInfo.CurrentTier.Asset}",
                                UpgradeText = sb.ToString()
                            });

                        if (pushEnabled && !noAmountTemplate)
                            pushTemplateTask = _templateFormatter.FormatAsync("PushTierUpgradedTemplate", clientAcc.PartnerId, "EN",
                                new { Tier = evt.Tier.ToString(), Amount = $"{tierInfo.CurrentTier.MaxLimit} {tierInfo.CurrentTier.Asset}"});

                        type = NotificationType.TierUpgraded.ToString();
                        break;
                    case KycStatus.NeedToFillData:
                        var documents = await _kycDocumentsService.GetCurrentDocumentsAsync(evt.ClientId);
                        var declinedDocuments = documents
                            .Where(item => item.Status.Name == CheckDocumentPorcess.DeclinedState.Name)
                            .ToArray();

                        if (declinedDocuments.Length > 0)
                        {
                            string documentsAsHtml = GetDocumentsInfo(declinedDocuments);
                            emailTemplateTask = _templateFormatter.FormatAsync("DeclinedDocumentsTemplate", clientAcc.PartnerId,
                                "EN", new { FullName = personalData.FullName, DocumentsAsHtml = documentsAsHtml, Year = DateTime.UtcNow.Year });
                        }

                        if (pushEnabled)
                            pushTemplateTask = _templateFormatter.FormatAsync("PushKycNeedDocumentsTemplate", clientAcc.PartnerId, "EN", new { });

                        type = NotificationType.KycNeedToFillDocuments.ToString();
                        break;
                    case KycStatus.Rejected:
                        emailTemplateTask = _templateFormatter.FormatAsync("TierUpgradeRejectedTemplate", clientAcc.PartnerId,
                            "EN", new { FullName = personalData.FullName, Tier = evt.Tier.ToString(), Year = DateTime.UtcNow.Year });
                        break;
                }
            }
            finally
            {
                await Task.WhenAll(emailTemplateTask, pushTemplateTask);

                var sendEmailTask = Task.CompletedTask;

                if (emailTemplateTask.Result != null)
                {
                    var msgData = new PlainTextData
                    {
                        Sender = personalData.Email,
                        Subject = emailTemplateTask.Result.Subject,
                        Text = emailTemplateTask.Result.HtmlBody
                    };

                    sendEmailTask = _emailSender.SendEmailAsync(clientAcc.PartnerId, personalData.Email, msgData);
                }

                if (pushEnabled && pushTemplateTask.Result != null)
                {
                    commandSender.SendCommand(new TextNotificationCommand
                    {
                        NotificationIds = new[]{clientAcc.NotificationsId},
                        Type = type,
                        Message = pushTemplateTask.Result.Subject
                    }, PushNotificationsBoundedContext.Name);
                }

                await sendEmailTask;
            }
        }

        private static string GetDocumentsInfo(IKycDocumentV2[] declinedDocuments)
        {
            var documentsAsHtml = new StringBuilder();
            foreach (var document in declinedDocuments)
            {
                string kycDocType = document.Type.Name;
                switch (document.Type.Name.ToLower())
                {
                    case "idcard":
                        kycDocType = "Passport or ID";
                        break;
                    case "idcardbackside":
                        kycDocType = "Passport or ID (back side)";
                        break;
                    case "proofofaddress":
                        kycDocType = "Proof of address";
                        break;
                }

                var comment = document.Status.Properties?["Reason"]?.ToObject<string>() ?? string.Empty;

                documentsAsHtml.AppendLine(
                    "<tr style='border-top: 1px solid #8C94A0; border-bottom: 1px solid #8C94A0;'>");
                documentsAsHtml.AppendLine(
                    $"<td style='padding: 15px 0 15px 0;' width='260'><span style='font-size: 1.1em;color: #8C94A0;'>{kycDocType}</span></td>");
                documentsAsHtml.AppendLine(
                    $"<td style='padding: 15px 0 15px 0;' width='260'><span style='font-size: 1.1em;color: #3F4D60;'>{comment.Replace("\r\n", "<br>")}</span></td>");
                documentsAsHtml.AppendLine("</tr>");
            }

            return documentsAsHtml.ToString();
        }
    }
}
