using common.tcpforward;
using server.messengers.register;

namespace server.service.tcpforward
{
    internal class TcpForwardTargetProvider : ITcpForwardTargetProvider
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;

        public TcpForwardTargetProvider(IClientRegisterCaching clientRegisterCaching, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
        }
        public TcpForwardTargetInfo Get(string host)
        {
           return GetTarget(tcpForwardTargetCaching.Get(host));
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
                cacheInfo.Connection = clientRegisterCaching.GetByName(cacheInfo.Name)?.TcpConnection;
            }
            return new TcpForwardTargetInfo
            {
                Connection = cacheInfo.Connection,
                Endpoint = cacheInfo.Endpoint,
            };
        }
    }
}