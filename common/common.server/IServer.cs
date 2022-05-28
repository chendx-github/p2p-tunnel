using common.libs;
using System;
using System.Net;

namespace common.server
{
    public interface IServer
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();

        public SimpleSubPushHandler<IConnection> OnPacket { get; }
    }
}
