using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models.Request.Settings;
using Lykke.Service.ClientAccount.Client.Models.Response.ClientAccountInformation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Limitations.Client.Events;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.PushNotifications.Contract.Enums;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;

namespace Lykke.Service.Tier.Workflow.Sagas
{
    public class ClientDepositsSaga
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly ILimitsService _limitsService;
        private readonly ISettingsService _settingsService;
        private readonly ITemplateFormatter _templateFormatter;
        private readonly IKycStatusService _kycStatusService;
        private readonly ITierUpgradeService _tierUpgradeService;

        public ClientDepositsSaga(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ILimitsService limitsService,
            ISettingsService settingsService,
            ITemplateFormatter templateFormatter,
            IKycStatusService kycStatusService,
            ITierUpgradeService tierUpgradeService
            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _limitsService = limitsService;
            _settingsService = settingsService;
            _templateFormatter = templateFormatter;
            _kycStatusService = kycStatusService;
            _tierUpgradeService = tierUpgradeService;
        }

        public async Task Handle(ClientDepositEvent evt, ICommandSender commandSender)
        {
            var clientAccountTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);
            var pdTask = _personalDataService.GetAsync(evt.ClientId);

            await Task.WhenAll(clientAccountTask, pdTask);

            ClientInfo clientAccount = clientAccountTask.Result;
            IPersonalData pd = pdTask.Result;

            if (clientAccount == null)
                return;

            await _limitsService.SaveDepositOperationAsync(evt);

            LimitSettings currentLimitSettings = await _limitsService.GetClientLimitSettingsAsync(evt.ClientId, clientAccount.Tier, pd.CountryFromPOA);

            if (currentLimitSettings?.MaxLimit == null)
                return;

            double checkAmount = await _limitsService.GetClientDepositAmountAsync(evt.ClientId, clientAccount.Tier);

            if (Math.Abs(checkAmount - currentLimitSettings.MaxLimit.Value) < 0.01)
            {
                await _clientAccountClient.ClientSettings.SetCashOutBlockAsync(new CashOutBlockRequest
                {
                    ClientId = evt.ClientId, CashOutBlocked = false, TradesBlocked = false
                });
            }

            if (checkAmount > currentLimitSettings.MaxLimit.Value)
            {
                var requests = await _tierUpgradeService.GetByClientAsync(evt.ClientId);

                var kycStatus = KycStatus.NeedToFillData;

                if (requests.Any())
                {
                    if (requests.Any(x => x.KycStatus == KycStatus.Pending))
                    {
                        kycStatus = KycStatus.Pending;
                    }

                    if (requests.Any(x => x.KycStatus == KycStatus.Rejected))
                    {
                        kycStatus = KycStatus.NeedToFillData;
                    }
                }
                else
                {
                    kycStatus = KycStatus.NeedToFillData;
                }

                await Task.WhenAll(
                    _kycStatusService.ChangeKycStatusAsync(evt.ClientId, kycStatus, $"{nameof(ClientDepositsSaga)} - limit reached ({checkAmount} of {currentLimitSettings.MaxLimit.Value} {_settingsService.GetDefaultAsset()})"),
                    _clientAccountClient.ClientSettings.SetCashOutBlockAsync(new CashOutBlockRequest
                    {
                        ClientId = evt.ClientId, CashOutBlocked = false, TradesBlocked = true
                    })
                );

            }

            if (checkAmount <= currentLimitSettings.MaxLimit.Value)
            {
                bool needNotification = _settingsService.IsLimitReachedForNotification(checkAmount, currentLimitSettings.MaxLimit.Value);

                if (!needNotification)
                    return;

                var pushSettings = await _clientAccountClient.ClientSettings.GetPushNotificationAsync(evt.ClientId);

                if (pushSettings.Enabled && !string.IsNullOrEmpty(clientAccount.NotificationsId))
                {
                    var template = await _templateFormatter.FormatAsync("PushLimitPercentReachedTemplate",
                        clientAccount.PartnerId, "EN",
                        new
                        {
                            CurrentAmount = checkAmount,
                            Limit = currentLimitSettings.MaxLimit.Value,
                            Percent = Math.Round(checkAmount / currentLimitSettings.MaxLimit.Value * 100),
                            FullName = pd.FullName,
                            Asset = evt.BaseAsset
                        });

                    commandSender.SendCommand(new TextNotificationCommand
                    {
                        NotificationIds = new[]{clientAccount.NotificationsId},
                        Type = NotificationType.DepositLimitPercentReached.ToString(),
                        Message = template.Subject
                    }, PushNotificationsBoundedContext.Name);
                }
            }
        }
    }
}
