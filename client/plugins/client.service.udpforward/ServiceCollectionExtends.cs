using common.libs;
using common.udpforward;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.udpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddUdpForwardPlugin(this ServiceCollection services)
        {
            services.AddSingleton<common.udpforward.Config>();

            services.AddSingleton<IUdpForwardServer, UdpForwardServer>();
            services.AddSingleton<IUdpForwardTargetProvider, UdpForwardTargetProvider>();
            services.AddSingleton<IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>, UdpForwardTargetCaching>();

            services.AddSingleton<UdpForwardTransfer>();
            services.AddSingleton<UdpForwardResolver>();
            services.AddSingleton<UdpForwardMessengerSender>();

            return services;
        }
        public static ServiceProvider UseUdpForwardPlugin(this ServiceProvider services)
        {
            services.GetService<UdpForwardTransfer>().StartP2P();
            services.GetService<UdpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Debug("Udp转发已加载");
            Logger.Instance.Debug("Udp转发已启动");
            var config = services.GetService<common.udpforward.Config>();

            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"udp转发已允许连接");
            }
            else
            {
                Logger.Instance.Info($"udp转发未允许连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            return services;
        }
    }
}
