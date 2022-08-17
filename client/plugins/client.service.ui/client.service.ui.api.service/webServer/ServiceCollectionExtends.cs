using common.libs;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.ui.api.service.webServer
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddWebServer(this ServiceCollection services)
        {
            services.AddSingleton<IWebServer, WebServer>();
            services.AddSingleton<IWebServerFileReader, WebServerFileReader>();

            return services;
        }
        public static ServiceProvider UseWebServer(this ServiceProvider services)
        {
            var config = services.GetService<Config>();

            if (config.EnableWeb)
            {
                services.GetService<IWebServer>().Start();
                Logger.Instance.Warning(string.Empty.PadRight(50, '='));
                Logger.Instance.Debug("管理UI，web已启用");
                Logger.Instance.Info($"管理UI web1 :http://{config.Web.BindIp}:{config.Web.Port}");
                Logger.Instance.Info($"管理UI web2 :https://snltty.gitee.io/p2p-tunnel");
                Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            }
            else
            {
                Logger.Instance.Debug("管理UI，web未启用");
            }

            return services;
        }
    }
}
