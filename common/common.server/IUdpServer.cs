using System.Net;

namespace common.server
{
    public interface IUdpServer : IServer
    {
        public IConnection CreateConnection(IPEndPoint address);
    }
}
