using client.service.vea;
using common.libs;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.tcpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddVirtualEthernetAdapterPlugin(this ServiceCollection services)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<VirtualEthernetAdapterTransfer>();
            return services;
        }
        public static ServiceProvider UseVirtualEthernetAdapterPlugin(this ServiceProvider services)
        {
            services.GetService<VirtualEthernetAdapterTransfer>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Debug("vea 虚拟网卡插件已加载");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            return services;
        }
    }
}
