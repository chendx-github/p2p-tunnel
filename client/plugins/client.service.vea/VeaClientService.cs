using client.service.ui.api.clientServer;
using common.libs.extends;

namespace client.service.vea
{
    public class VeaClientService : IClientService
    {
        private readonly Config config;
        private readonly VirtualEthernetAdapterTransfer virtualEthernetAdapterTransfer;

        public VeaClientService(Config config, VirtualEthernetAdapterTransfer virtualEthernetAdapterTransfer)
        {
            this.config = config;
            this.virtualEthernetAdapterTransfer = virtualEthernetAdapterTransfer;
        }

        public Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }

        public void Set(ClientServiceParamsInfo arg)
        {
            var conf = arg.Content.DeJson<Config>();

            config.Enable = conf.Enable;
            config.ProxyAll = conf.ProxyAll;
            config.TargetName = conf.TargetName;
            config.IP = conf.IP;
            config.SocksPort = conf.SocksPort;

            virtualEthernetAdapterTransfer.Run();

            config.SaveConfig().Wait();
        }
    }
}
