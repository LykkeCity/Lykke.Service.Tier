using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Lykke.Common;
using Lykke.Common.Log;
using Lykke.HttpClientGenerator.Infrastructure;
using Lykke.Logs;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.Tier.AzureRepositories;
using Lykke.Service.Tier.Domain;
using Lykke.Service.Tier.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Tier.LimitUpdater
{
    public class Program
    {
        //THIS CODE
        //1. Backups limit storage to new table
        //2. Delete items in limit storage if all the following conditions match: 
        // - user is in Advanced tier
        // - user's country is low risk
        public static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidOperationException(
                    "Requires exactly one command line arg with settings url to Lykke.Service.Tier. [dotnet run <settingsUrl>]");
            }
            
            Environment.SetEnvironmentVariable("ENV_INFO", "nonsence");

            const string SettingsUrlKey = "SettingsUrl";
            var configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    {SettingsUrlKey, args[0]}
                })
                .Build();

            var settings = configurationRoot.LoadSettings<AppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications?.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications?.AzureQueue.QueueName);
                options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            }, SettingsUrlKey);
            
            
            var logFactory = new ServiceCollection()
                .AddLykkeLogging(settings.ConnectionString(x=>x.TierService.Db.LogsConnString),
                "TierLimitUpdater",
                settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                settings.CurrentValue.SlackNotifications.AzureQueue.QueueName,
                options => {  })
                .BuildServiceProvider().GetRequiredService<ILogFactory>();
            var logger = logFactory.CreateLog(new Program());

            var personalDataService = new PersonalDataService(settings.CurrentValue.PersonalDataServiceClient, logFactory);            
            
            var clientAccountService = new ClientAccountClient(Lykke.HttpClientGenerator.HttpClientGenerator
                .BuildForUrl(settings.CurrentValue.ClientAccountServiceClient.ServiceUrl)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper()).WithoutRetries()
                .Create());
            
            var limitStorage = AzureTableStorage<LimitEntity>.Create(
                settings.ConnectionString(x => x.TierService.Db.DataConnString),
                "IndividualLimits", logFactory);


            
            var backupStorageTableName = $"IndividualLimitsBackup{DateTime.Now.Ticks}";
            var backupStorage = AzureTableStorage<LimitEntity>.Create(
                settings.ConnectionString(x => x.TierService.Db.DataConnString),
                backupStorageTableName, logFactory);

            logger.Info("Reading items");
            var existedLimits = new List<LimitEntity>();
            await limitStorage.GetDataByChunksAsync(limits =>
            {
                existedLimits.AddRange(limits);
                logger.Info($"{existedLimits.Count} read");
            });
            
            logger.Info($"Backup data to {backupStorageTableName}");
            var backupCounter = 0;
            foreach (var batch in existedLimits)
            {
                await backupStorage.InsertAsync(batch);
                backupCounter += 1;
                logger.Info($"{backupCounter} of {existedLimits.Count} backed up to {backupStorageTableName}");
            }
            
            logger.Info($"Select items for deletion");
            var selectCount = 0;
            var selectedForDeletion = new List<LimitEntity>();
            var lowRiskCountries = settings.CurrentValue.TierService.Countries[CountryRisk.Low].ToDictionary(p => p);
            foreach (var limit in existedLimits)
            {
                var personalData = await personalDataService.GetAsync(limit.ClientId);
                var clientAccount = await clientAccountService.ClientAccountInformation.GetByIdAsync(limit.ClientId);
                if (personalData == null)
                {
                    throw new InvalidOperationException($"Personal data is null for {limit.ClientId}");
                }
                if (clientAccount == null)
                {
                    throw new InvalidOperationException($"Personal data is null for {limit.ClientId}");
                }


                var shouldDelete = clientAccount.Tier == AccountTier.Advanced &&
                                   lowRiskCountries.ContainsKey(personalData.CountryFromID) &&
                                   lowRiskCountries.ContainsKey(personalData.CountryFromPOA);
                if (shouldDelete)
                {
                    selectedForDeletion.Add(limit);
                }
                selectCount++;
                var resolution = shouldDelete ? "DELETE" : "DO NOT TOUCH";
                logger.Info($"{selectCount} of {existedLimits.Count}. " +
                            $"ClientId: {limit.ClientId}, Tier: {clientAccount.Tier}, CountryFromID: {personalData.CountryFromID}, CountryFromPOA: {personalData.CountryFromPOA}" +
                            $"Resolution : {resolution}");

            }
            
            logger.Info($"Deleting {selectedForDeletion.Count} items");
            var deleteCounter = 0;
            foreach (var limitEntity in selectedForDeletion)
            {
                await limitStorage.DeleteAsync(limitEntity);
                deleteCounter++;
                logger.Info($"{deleteCounter} of {selectedForDeletion.Count} deleted");
            }
            
            logger.Info("All DONE");
        }
    }
}
