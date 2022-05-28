using common.libs;
using common.tcpforward;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.tcpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection services)
        {
            services.AddSingleton<common.tcpforward.Config>();

            services.AddSingleton<ITcpForwardServer, TcpForwardServerPre>();
            services.AddSingleton<ITcpForwardTargetProvider, TcpForwardTargetProvider>();
            services.AddSingleton<ITcpForwardTargetCaching<TcpForwardTargetCacheInfo>, TcpForwardTargetCaching>();

            services.AddSingleton<TcpForwardTransfer>();
            services.AddSingleton<TcpForwardResolver>();
            services.AddSingleton<TcpForwardMessengerSender>();

            return services;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider services)
        {
            services.GetService<TcpForwardTransfer>().StartP2P();
            services.GetService<TcpForwardResolver>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Debug("tcp转发、http1.1代理已加载");
            Logger.Instance.Debug("tcp转发、http1.1代理已启动");
            var config = services.GetService<common.tcpforward.Config>();
            
            if (config.LanConnectEnable)
            {
                Logger.Instance.Debug($"http1.1代理已允许本地连接");
            }
            else
            {
                Logger.Instance.Info($"http1.1代理未允许本地连接");
            }
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            return services;
        }
    }
}
