using Microsoft.Extensions.DependencyInjection;
using common.libs;
using common.udpforward;

namespace server.service.udpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddUdpForwardPlugin(this ServiceCollection services)
        {
            services.AddSingleton<common.udpforward.Config>();//启动器
            services.AddSingleton<IUdpForwardServer, UdpForwardServer>(); //监听服务
            services.AddSingleton<UdpForwardMessengerSender>(); //消息发送
            services.AddSingleton<UdpForwardTransfer>();//启动器

            services.AddSingleton<IUdpForwardTargetProvider, UdpForwardTargetProvider>(); //目标提供器
            services.AddSingleton<IUdpForwardTargetCaching<UdpForwardTargetCacheInfo>, UdpForwardTargetCaching>(); //转发缓存器
            services.AddSingleton<UdpForwardResolver>();

            return services;
        }
        public static ServiceProvider UseUdpForwardPlugin(this ServiceProvider services)
        {
            services.GetService<UdpForwardTransfer>();
            services.GetService<UdpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Info($"udp转发已加载");
            var config = services.GetService<common.udpforward.Config>();
            if (config.ConnectEnable)
            {
                Logger.Instance.Debug($"udp转发已允许注册");
            }
            else
            {
                Logger.Instance.Info($"udp转发未允许注册");
            }
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            return services;
        }
    }
}
