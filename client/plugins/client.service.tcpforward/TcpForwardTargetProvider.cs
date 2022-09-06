﻿using client.messengers.clients;
using client.messengers.register;
using common.libs;
using common.server;
using common.tcpforward;
using System.Threading;

namespace client.service.tcpforward
{
    internal class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly RegisterStateInfo registerStateInfo;
        IClientsTransfer clientsTransfer;

        public TcpForwardTargetProvider(IClientsTransfer clientsTransfer, IClientInfoCaching clientInfoCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.registerStateInfo = registerStateInfo;
            this.clientsTransfer = clientsTransfer;
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                tcpForwardTargetCaching.ClearConnection();
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                tcpForwardTargetCaching.ClearConnection(client.Name);
            });
        }

        public void Get(string domain, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(domain), info);
        }
        public void Get(int port, TcpForwardInfo info)
        {
            GetTarget(tcpForwardTargetCaching.Get(port), info);
        }

        private void GetTarget(TcpForwardTargetCacheInfo cacheInfo, TcpForwardInfo info)
        {
            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || !cacheInfo.Connection.Connected)
                {
                    cacheInfo.Connection = SelectConnection(cacheInfo);
                }
                info.Connection = cacheInfo.Connection;
                info.TargetEndpoint = cacheInfo.Endpoint;
            }
        }
        /// <summary>
        /// 选择连接对象
        /// </summary>
        /// <param name="cacheInfo"></param>
        /// <returns></returns>
        private IConnection SelectConnection(TcpForwardTargetCacheInfo cacheInfo)
        {
            if (string.IsNullOrWhiteSpace(cacheInfo.Name))//判断name是不是空的 看看是不是需要链接到服务器
            {
                return cacheInfo.TunnelType switch
                {
                    TcpForwardTunnelTypes.TCP_FIRST => registerStateInfo.TcpConnection ?? registerStateInfo.UdpConnection,
                    TcpForwardTunnelTypes.UDP_FIRST => registerStateInfo.UdpConnection ?? registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.TCP => registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.UDP => registerStateInfo.UdpConnection,
                    _ => registerStateInfo.UdpConnection,
                };
            }

            ClientInfo client = clientInfoCaching.GetByName(cacheInfo.Name);
            if (client == null)
            {
                return null;
            }

            //这里应该尝试打洞
            var out1 = cacheInfo.TunnelType switch
            {
                TcpForwardTunnelTypes.TCP_FIRST => client.TcpConnection ?? client.UdpConnection,
                TcpForwardTunnelTypes.UDP_FIRST => client.UdpConnection ?? client.TcpConnection,
                TcpForwardTunnelTypes.TCP => client.TcpConnection,
                TcpForwardTunnelTypes.UDP => client.UdpConnection,
                _ => client.UdpConnection,
            };
            if (out1 == null && !(client.UdpConnecting || client.TcpConnecting))
            {
                Logger.Instance.Debug("正在尝试打洞");
                int i1 = 0;
                clientsTransfer.ConnectClient(client.Id);
                do
                {
                    Thread.Sleep(100);
                } while (i1++ < 10 && (client.UdpConnecting || client.TcpConnecting));
                out1 = cacheInfo.TunnelType switch
                {
                    TcpForwardTunnelTypes.TCP_FIRST => client.TcpConnection ?? client.UdpConnection,
                    TcpForwardTunnelTypes.UDP_FIRST => client.UdpConnection ?? client.TcpConnection,
                    TcpForwardTunnelTypes.TCP => client.TcpConnection,
                    TcpForwardTunnelTypes.UDP => client.UdpConnection,
                    _ => client.UdpConnection,
                };
            }


            return out1;
        }
    }
}