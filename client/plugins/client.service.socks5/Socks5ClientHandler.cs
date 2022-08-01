using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.socks5;

namespace client.service.socks5
{
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        private readonly Socks5MessengerSender socks5MessengerSender;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly common.socks5.Config config;
        private IConnection connection;
        private IClientInfoCaching clientInfoCaching;

        public Socks5ClientHandler(Socks5MessengerSender socks5MessengerSender, RegisterStateInfo registerStateInfo, common.socks5.Config config, IClientInfoCaching clientInfoCaching)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.registerStateInfo = registerStateInfo;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
        }

        public bool HandleRequest(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        public bool HandleAuth(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Auth(data, connection);
        }

        public bool HandleCommand(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Command(data, connection);
        }
        public bool HndleForward(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Forward(data, connection);
        }
        public bool HndleForwardUdp(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.ForwardUdp(data, connection);
        }

        public void Close(ulong id)
        {
            GetConnection();
            socks5MessengerSender.RequestClose(id, connection);
        }
        public void Flush()
        {
            connection = null;
            GetConnection();
        }

        private void GetConnection()
        {
            if (connection == null)
            {
                if (string.IsNullOrWhiteSpace(config.TargetName))
                {
                    connection = SelectConnection(config.TunnelType, registerStateInfo.TcpConnection, registerStateInfo.UdpConnection);
                }
                else
                {
                    var client = clientInfoCaching.GetByName(config.TargetName);
                    if(client != null)
                    {
                        connection = SelectConnection(config.TunnelType, client.TcpConnection, client.UdpConnection);
                    }
                }
            }
        }
        private IConnection SelectConnection(TunnelTypes tunnelType, IConnection tcpconnection, IConnection udpconnection)
        {
            return tunnelType switch
            {
                TunnelTypes.TCP_FIRST => tcpconnection != null ? tcpconnection : udpconnection,
                TunnelTypes.UDP_FIRST => udpconnection != null ? udpconnection : tcpconnection,
                TunnelTypes.TCP => tcpconnection,
                TunnelTypes.UDP => udpconnection,
                _ => tcpconnection,
            };
        }


    }
}
