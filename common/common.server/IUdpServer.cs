using System.Net;

namespace common.server
{
    public interface IUdpServer : IServer
    {
        public IConnection CreateConnection(IPEndPoint address);
        public int 心跳发送失败次数 { get; set; }
    }
}
