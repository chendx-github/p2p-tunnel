using common.libs;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.socks5
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddSocks5(this ServiceCollection services)
        {

            services.AddSingleton<Socks5Transfer>();
            services.AddSingleton<common.socks5.Config>();
            services.AddSingleton<Socks5ClientListener>();
            services.AddSingleton<Socks5MessengerSender>();

            services.AddSingleton<ISocks5ServerHandler, Socks5ServerHandler>();
            services.AddSingleton<ISocks5ClientHandler, Socks5ClientHandler>();

            return services;
        }

        public static ServiceProvider UseSocks5(this ServiceProvider services)
        {
            common.socks5.Config config = services.GetService<common.socks5.Config>();
            Socks5Transfer socks5Transfer = services.GetService<Socks5Transfer>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Debug($"socks5已加载");
            if (config.ListenEnable)
            {
                services.GetService<Socks5ClientListener>().Start(config.ListenPort);
                if (config.IsPac)
                {
                    socks5Transfer.UpdatePac(new PacSetParamsInfo { IsCustom = config.IsCustomPac });
                }
                Logger.Instance.Debug($"socks5已监听 socks5://127.0.0.1:{config.ListenPort}");
            }
            else
            {
                Logger.Instance.Info($"socks5未监听");
            }
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
