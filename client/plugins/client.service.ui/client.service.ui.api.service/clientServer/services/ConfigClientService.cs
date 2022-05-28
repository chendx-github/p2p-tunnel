using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    public class ConfigClientService : IClientService
    {
        private readonly client.Config config;
        public ConfigClientService(client.Config config)
        {
            this.config = config;
        }

        public async Task Update(ClientServiceParamsInfo arg)
        {
            ConfigureParamsInfo model = arg.Content.DeJson<ConfigureParamsInfo>();

            config.Client = model.ClientConfig;
            config.Server = model.ServerConfig;
            await config.SaveConfig().ConfigureAwait(false);
        }
    }

    public class ConfigureParamsInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
    }
}
