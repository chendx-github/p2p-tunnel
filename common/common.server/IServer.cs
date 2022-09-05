using common.libs;
using System;
using System.Net;

namespace common.server
{
    public interface IServer
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();
        /// <summary>
        /// 小包裹数据
        /// </summary>
        public SimpleSubPushHandler<IConnection> OnPacket { get; }
        /// <summary>
        /// 断开链接
        /// </summary>
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; }
        public Action<IConnection> OnConnected { get; set; }
    }
}
