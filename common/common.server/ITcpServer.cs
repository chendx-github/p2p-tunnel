using System;
using System.Net;
using System.Net.Sockets;

namespace common.server
{
    public interface ITcpServer : IServer
    {
        public void SetBufferSize(int bufferSize = 8*1024);
        public Socket BindAccept(int port, IPAddress ip);
        public IConnection BindReceive(Socket socket, Action<SocketError> errorCallback = null, int bufferSize = 8 * 1024);

        public IConnection CreateConnection(Socket socket);
    }
}
