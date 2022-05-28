using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.tcpforward;
using System;

namespace client.service.tcpforward
{
    internal class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly RegisterStateInfo registerStateInfo;

        public TcpForwardTargetProvider(IClientInfoCaching clientInfoCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.registerStateInfo = registerStateInfo;
        }

        public TcpForwardTargetInfo Get(string domain)
        {
            return GetTarget(tcpForwardTargetCaching.Get(domain));
        }
        public TcpForwardTargetInfo Get(int port)
        {
            return GetTarget(tcpForwardTargetCaching.Get(port));
        }

        private TcpForwardTargetInfo GetTarget(TcpForwardTargetCacheInfo cacheInfo)
        {
            if (cacheInfo == null)
            {
                return new TcpForwardTargetInfo { };
            }

            if (cacheInfo.Connection == null)
            {
                cacheInfo.Connection = SelectConnection(cacheInfo);
            }
            return new TcpForwardTargetInfo
            {
                Connection = cacheInfo.Connection,
                Endpoint = cacheInfo.Endpoint,
            };
        }

        private IConnection SelectConnection(TcpForwardTargetCacheInfo cacheInfo)
        {
            if (string.IsNullOrWhiteSpace(cacheInfo.Name))
            {
                return cacheInfo.TunnelType switch
                {
                    TcpForwardTunnelTypes.TCP_FIRST => registerStateInfo.TcpConnection != null ? registerStateInfo.TcpConnection : registerStateInfo.UdpConnection,
                    TcpForwardTunnelTypes.UDP_FIRST => registerStateInfo.UdpConnection != null ? registerStateInfo.UdpConnection : registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.TCP => registerStateInfo.TcpConnection,
                    TcpForwardTunnelTypes.UDP => registerStateInfo.UdpConnection,
                    _ => registerStateInfo.TcpConnection,
                };
            }

            ClientInfo client = clientInfoCaching.GetByName(cacheInfo.Name);
            if (client == null)
            {
                return null;
            }

            return cacheInfo.TunnelType switch
            {
                TcpForwardTunnelTypes.TCP_FIRST => client.TcpConnection != null ? client.TcpConnection : client.UdpConnection,
                TcpForwardTunnelTypes.UDP_FIRST => client.UdpConnection != null ? client.UdpConnection : client.TcpConnection,
                TcpForwardTunnelTypes.TCP => client.TcpConnection,
                TcpForwardTunnelTypes.UDP => client.UdpConnection,
                _ => client.TcpConnection,
            };
        }
    }
}