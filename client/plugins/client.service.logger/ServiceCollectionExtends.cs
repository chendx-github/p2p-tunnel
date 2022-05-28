using common.libs;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.logger
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddLoggerPlugin(this ServiceCollection services)
        {
            services.AddSingleton<Config>();

            return services;
        }

        public static ServiceProvider UseLoggerPlugin(this ServiceProvider services)
        {
            LoggerClientService plugin = services.GetService<LoggerClientService>();
            Config config = services.GetService<Config>();
            Logger.Instance.OnLogger.Sub((model) =>
            {
                if (config.Enable)
                {
                    plugin.Data.Add(model);
                    if (plugin.Data.Count > config.MaxLength)
                    {
                        plugin.Data.RemoveAt(0);
                    }
                }
            });

            Logger.Instance.Debug("日志收集插件已加载");
            return services;
        }
    }

}
