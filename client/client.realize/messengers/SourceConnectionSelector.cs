using client.messengers.clients;
using common.server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.realize.messengers
{
    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public SourceConnectionSelector(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public IConnection Select(IConnection connection)
        {
            if (connection.ReceiveRequestWrap.Delay == 1)
            {
                if (clientInfoCaching.Get(connection.ReceiveRequestWrap.DelayId, out ClientInfo client))
                {
                    return connection.ServerType == common.server.model.ServerType.TCP ? client.TcpConnection : client.UdpConnection;
                }
            }
            return connection;
        }
    }
}
