﻿using common.server;
using common.socks5;
using System.Threading.Tasks;

namespace server.service.socks5
{
    /// <summary>
    /// socks5消息
    /// </summary>
    [MessengerIdRange((ushort)Socks5MessengerIds.Min, (ushort)Socks5MessengerIds.Max)]
    public class Socks5Messenger : IMessenger
    {
        private readonly ISocks5ServerHandler socks5ServerHandler;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5ServerHandler"></param>
        public Socks5Messenger(ISocks5ServerHandler socks5ServerHandler)
        {
            this.socks5ServerHandler = socks5ServerHandler;
        }

        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)Socks5MessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Tag = connection;
            data.ClientId = connection.FromConnection.ConnectId;
            await socks5ServerHandler.InputData(data);
        }
    }
}
