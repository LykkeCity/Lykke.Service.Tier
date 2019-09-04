using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Messages.Email.MessageData;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EmailSender;
using Lykke.Service.Kyc.Abstractions.Domain.Documents;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.PushNotifications.Contract.Enums;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.Contract;
using Lykke.Service.Tier.Domain.Events;
using Lykke.Service.Tier.Domain.Services;
using IEmailSender = Lykke.Messages.Email.IEmailSender;

namespace Lykke.Service.Tier.Workflow.Projections
{
    public class TierUpgradeRequestProjection
    {
        public ICqrsEngine CqrsEngine { get; set; }

        private readonly ITierUpgradeService _tierUpgradeService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycDocumentsServiceV2 _kycDocumentsService;
        private readonly IEmailSender _emailSender;
        private readonly ITemplateFormatter _templateFormatter;

        public TierUpgradeRequestProjection(
            ITierUpgradeService tierUpgradeService,
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            IKycDocumentsServiceV2 kycDocumentsService,
            IEmailSender emailSender,
            ITemplateFormatter templateFormatter
        )
        {
            _tierUpgradeService = tierUpgradeService;
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _kycDocumentsService = kycDocumentsService;
            _emailSender = emailSender;
            _templateFormatter = templateFormatter;
        }

        public Task Handle(TierUpgradeRequestChangedEvent evt)
        {
            return Task.WhenAll(
                _tierUpgradeService.UpdateCountsAsync(evt.ClientId, evt.Tier, evt.OldStatus, evt.NewStatus),
                SendNotificationAsync(evt)
            );
        }

        private async Task SendNotificationAsync(TierUpgradeRequestChangedEvent evt)
        {
            var clientAccTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);
            var personalDataTask = _personalDataService.GetAsync(evt.ClientId);
            var pushSettingsTask = _clientAccountClient.ClientSettings.GetPushNotificationAsync(evt.ClientId);

            await Task.WhenAll(clientAccTask, personalDataTask, pushSettingsTask);

            var clientAcc = clientAccTask.Result;
            var personalData = personalDataTask.Result;
            var pushSettings = pushSettingsTask.Result;
            bool pushEnabled = pushSettings.Enabled && !string.IsNullOrEmpty(clientAcc.NotificationsId);

            Task emailTask = null;
            Task<EmailMessage> emailTemplateTask = Task.FromResult<EmailMessage>(null);
            Task<EmailMessage> pushTemplateTask = Task.FromResult<EmailMessage>(null);
            string type = string.Empty;

            try
            {
                switch (evt.NewStatus)
                {
                    case KycStatus.Ok:
                        emailTemplateTask = _templateFormatter.FormatAsync("TierUpgradedTemplate", clientAcc.PartnerId,
                            "EN", new { FullName = personalData.FullName, Tier = evt.Tier.ToString(), Year = DateTime.UtcNow.Year });

                        if (pushEnabled)
                            pushTemplateTask = _templateFormatter.FormatAsync("PushTierUpgradedTemplate", clientAcc.PartnerId, "EN", new { Tier = evt.Tier.ToString() });

                        type = NotificationType.Info.ToString();
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
//                        emailTemplateTask = _templateFormatter.FormatAsync("TierUpgradedTemplate", clientAcc.PartnerId,
//                            "EN", new { FullName = personalData.FullName, Tier = notification.Tier.ToString(), Year = DateTime.UtcNow.Year });
                        break;

                    case KycStatus.RestrictedArea:
                        emailTemplateTask = _templateFormatter.FormatAsync("RestrictedAreaTemplate", clientAcc.PartnerId,
                            "EN", new { FirstName = personalData.FirstName, LastName = personalData.LastName, Year = DateTime.UtcNow.Year });

                        if (pushEnabled)
                            pushTemplateTask = _templateFormatter.FormatAsync("PushKycRestrictedTemplate", clientAcc.PartnerId, "EN", new { });

                        type = NotificationType.KycRestrictedArea.ToString();
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
                    CqrsEngine.SendCommand(new TextNotificationCommand
                    {
                        NotificationIds = new[]{clientAcc.NotificationsId},
                        Type = type,
                        Message = pushTemplateTask.Result.Subject
                    }, TierBoundedContext.Name, PushNotificationsBoundedContext.Name);
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
