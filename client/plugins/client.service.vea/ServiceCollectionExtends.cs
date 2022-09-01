using client.service.vea;
using common.libs;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.tcpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddVirtualEthernetAdapterPlugin(this ServiceCollection services)
        {
            services.AddSingleton<vea.Config>();
            services.AddSingleton<VirtualEthernetAdapterTransfer>();
            services.AddSingleton<VeaMessengerSender>();
            services.AddSingleton<IVeaSocks5ClientHandler, VeaSocks5ClientHandler>();
            services.AddSingleton<IVeaSocks5ClientListener, VeaSocks5ClientListener>();

            return services;
        }
        public static ServiceProvider UseVirtualEthernetAdapterPlugin(this ServiceProvider services)
        {
            var transfer = services.GetService<VirtualEthernetAdapterTransfer>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Debug("vea 虚拟网卡插件已加载");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            transfer.Run();

            return services;
        }
    }
}
