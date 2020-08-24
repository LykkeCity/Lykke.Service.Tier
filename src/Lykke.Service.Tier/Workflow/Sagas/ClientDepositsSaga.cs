using System;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Cqrs;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models.Response.ClientAccountInformation;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using Lykke.Service.PushNotifications.Contract;
using Lykke.Service.PushNotifications.Contract.Commands;
using Lykke.Service.PushNotifications.Contract.Enums;
using Lykke.Service.TemplateFormatter.Client;
using Lykke.Service.Tier.Domain.Deposits;
using Lykke.Service.Tier.Domain.Services;
using Lykke.Service.Tier.Domain.Settings;
using Lykke.Service.Tier.Workflow.Events;

namespace Lykke.Service.Tier.Workflow.Sagas
{
    public class ClientDepositsSaga
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly ILimitsService _limitsService;
        private readonly ISettingsService _settingsService;
        private readonly ITemplateFormatter _templateFormatter;
        private readonly IMapper _mapper;

        public ClientDepositsSaga(
            IClientAccountClient clientAccountClient,
            IPersonalDataService personalDataService,
            ILimitsService limitsService,
            ISettingsService settingsService,
            ITemplateFormatter templateFormatter,
            IMapper mapper
            )
        {
            _clientAccountClient = clientAccountClient;
            _personalDataService = personalDataService;
            _limitsService = limitsService;
            _settingsService = settingsService;
            _templateFormatter = templateFormatter;
            _mapper = mapper;
        }

        public async Task Handle(ClientDepositedEvent evt, ICommandSender commandSender)
        {
            var clientAccountTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(evt.ClientId);
            var pdTask = _personalDataService.GetAsync(evt.ClientId);

            await Task.WhenAll(clientAccountTask, pdTask);

            ClientInfo clientAccount = clientAccountTask.Result;
            IPersonalData pd = pdTask.Result;

            if (clientAccount == null)
                return;

            await _limitsService.SaveDepositOperationAsync(_mapper.Map<DepositOperation>(evt));

            LimitSettings currentLimitSettings = await _limitsService.GetClientLimitSettingsAsync(evt.ClientId, clientAccount.Tier, pd.CountryFromPOA);

            if (currentLimitSettings?.MaxLimit == null)
                return;

            var checkAmountTask = _limitsService.GetClientDepositAmountAsync(evt.ClientId);
            var pushSettingsTask = _clientAccountClient.ClientSettings.GetPushNotificationAsync(evt.ClientId);

            await Task.WhenAll(checkAmountTask, pushSettingsTask);

            var checkAmount = checkAmountTask.Result;
            var pushSettings = pushSettingsTask.Result;

            if (checkAmount > currentLimitSettings.MaxLimit.Value)
            {
                await _limitsService.SetLimitReachedAsync(evt.ClientId, checkAmount,
                    currentLimitSettings.MaxLimit.Value, evt.BaseAsset);

                if (pushSettings.Enabled && !string.IsNullOrEmpty(clientAccount.NotificationsId))
                    await SendPushNotificationAsync(clientAccount.PartnerId, clientAccount.NotificationsId,
                        "PushLimitReachedTemplate", new { }, commandSender);
            }

            if (checkAmount <= currentLimitSettings.MaxLimit.Value)
            {
                bool needNotification = _settingsService.IsLimitReachedForNotification(checkAmount, currentLimitSettings.MaxLimit.Value);

                if (!needNotification)
                    return;

                if (pushSettings.Enabled && !string.IsNullOrEmpty(clientAccount.NotificationsId))
                    await SendPushNotificationAsync(clientAccount.PartnerId, clientAccount.NotificationsId,
                        "PushLimitPercentReachedTemplate", new {
                            CurrentAmount = checkAmount,
                            Limit = currentLimitSettings.MaxLimit.Value,
                            Percent = Math.Round(checkAmount / currentLimitSettings.MaxLimit.Value * 100),
                            FullName = pd.FullName,
                            Asset = evt.BaseAsset
                        }, commandSender);
            }
        }

        private async Task SendPushNotificationAsync(string partnerId, string notificationId, string template, object model, ICommandSender commandSender)
        {
            var message = await _templateFormatter.FormatAsync(template, partnerId, "EN", model);

            commandSender.SendCommand(new TextNotificationCommand
            {
                NotificationIds = new[]{notificationId},
                Type = NotificationType.DepositLimitPercentReached.ToString(),
                Message = message.Subject
            }, PushNotificationsBoundedContext.Name);
        }
    }
}
