using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System;
using client.messengers.clients;

namespace client.service.vea
{
    public class VeaClientService : IClientService
    {
        private readonly Config config;
        private readonly VirtualEthernetAdapterTransfer virtualEthernetAdapterTransfer;
        private readonly VeaMessengerSender veaMessengerSender;
        private readonly IClientInfoCaching clientInfoCaching;

        public VeaClientService(Config config, VirtualEthernetAdapterTransfer virtualEthernetAdapterTransfer, VeaMessengerSender veaMessengerSender, IClientInfoCaching clientInfoCaching)
        {
            this.config = config;
            this.virtualEthernetAdapterTransfer = virtualEthernetAdapterTransfer;
            this.veaMessengerSender = veaMessengerSender;
            this.clientInfoCaching = clientInfoCaching;
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

            try
            {
                virtualEthernetAdapterTransfer.Run();
            }
            catch (Exception ex)
            {
                arg.SetCode(ClientServiceResponseCodes.Error, ex.Message);
            }

            config.SaveConfig().Wait();
        }


        public async Task<Dictionary<ulong, IPAddress>> Update(ClientServiceParamsInfo arg)
        {
            var ids = arg.Content.DeJson<ulong[]>();

            Dictionary<ulong, IPAddress> res = new Dictionary<ulong, IPAddress>();
            if (ids.Any())
            {
                var clients = clientInfoCaching.All().Where(c => ids.Contains(c.Id));
                if (clients.Any())
                {
                    foreach (var item in clients)
                    {
                        var ip = await veaMessengerSender.IP(item.OnlineConnection);
                        res.Add(item.Id, ip);
                    }
                }
            }

            return res;
        }
    }
}
