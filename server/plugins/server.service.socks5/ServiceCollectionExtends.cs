using common.libs;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;

namespace server.service.socks5
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddSocks5(this ServiceCollection services)
        {
            services.AddSingleton<common.socks5.Config>();

            services.AddSingleton<Socks5ClientListener>();
            services.AddSingleton<Socks5MessengerSender>();

            services.AddSingleton<ISocks5ServerHandler, Socks5ServerHandler>();
            services.AddSingleton<ISocks5ClientHandler, Socks5ClientHandler>();

            return services;
        }

        public static ServiceProvider UseSocks5(this ServiceProvider services)
        {
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Info("socks5已加载");
            Config config = services.GetService<Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"socks5已允许连接");
            }
            else
            {
                Logger.Instance.Info($"socks5未允许连接");
            }
            if (config.LanConnectEnable)
            {
                Logger.Instance.Debug($"socks5已允许本地连接");
            }
            else
            {
                Logger.Instance.Info($"socks5未允许本地连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            return services;
        }
    }

}
