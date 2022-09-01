using common.libs;
using common.server;

namespace client.service.vea
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class VeaMessenger : IMessenger
    {
        private readonly Config config;
        public VeaMessenger(Config config)
        {
            this.config = config;
        }

        public byte[] IP(IConnection connection)
        {
            return config.Enable ? config.IP.GetAddressBytes() : Helper.EmptyArray;
        }
    }
}
