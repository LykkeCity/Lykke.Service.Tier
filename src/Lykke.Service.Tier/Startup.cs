using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.Tier.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Lykke.Service.Tier
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "Tier API",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.ConfigureMvcOptions = mvcOptions =>
                {
                    var formatter =
                        mvcOptions.OutputFormatters.FirstOrDefault(i => i.GetType() == typeof(JsonOutputFormatter));
                    var jsonFormatter = formatter as JsonOutputFormatter;
                    var formatterSettings = jsonFormatter == null
                        ? JsonSerializerSettingsProvider.CreateSerializerSettings()
                        : jsonFormatter.PublicSerializerSettings;
                    if (formatter != null)
                        mvcOptions.OutputFormatters.RemoveType<JsonOutputFormatter>();
                    formatterSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    JsonOutputFormatter jsonOutputFormatter =
                        new JsonOutputFormatter(formatterSettings, ArrayPool<char>.Create());
                    mvcOptions.OutputFormatters.Insert(0, jsonOutputFormatter);
                };

                options.Logs = logs =>
                {
                    logs.AzureTableName = "TierLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.TierService.Db.LogsConnString;
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }
    }
}
