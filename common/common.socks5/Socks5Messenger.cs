using common.server;
using System;

namespace common.socks5
{
    public class Socks5Messenger : IMessenger
    {
        private readonly Socks5ClientListener socks5ClientListener;
        private readonly ISocks5ServerHandler socks5ServerHandler;


        public Socks5Messenger(Socks5ClientListener socks5ClientListener, ISocks5ServerHandler socks5ServerHandler)
        {
            this.socks5ClientListener = socks5ClientListener;
            this.socks5ServerHandler = socks5ServerHandler;
        }

        public void Request(IConnection connection)
        {
            Socks5Info socks5Info = new Socks5Info();
            socks5Info.DeBytes(connection.ReceiveRequestWrap.Memory);
            socks5ServerHandler.HandleRequest(connection, socks5Info);
        }
        public void RequestResponse(IConnection connection)
        {
            (ulong id, Memory<byte> data) = Socks5Info.Read(connection.ReceiveRequestWrap.Memory);
            socks5ClientListener.RequestResponse(id, (Socks5EnumAuthType)data.Span[0]);
        }

        public void Auth(IConnection connection)
        {
            Socks5Info socks5Info = new Socks5Info();
            socks5Info.DeBytes(connection.ReceiveRequestWrap.Memory);
            socks5ServerHandler.HandleAuth(connection, socks5Info);
        }
        public void AuthResponse(IConnection connection)
        {
            (ulong id, Memory<byte> data) = Socks5Info.Read(connection.ReceiveRequestWrap.Memory);
            socks5ClientListener.AuthResponse(id, (Socks5EnumAuthState)data.Span[0]);
        }

        public void Command(IConnection connection)
        {
            Socks5Info socks5Info = new Socks5Info();
            socks5Info.DeBytes(connection.ReceiveRequestWrap.Memory);
            socks5ServerHandler.HandleCommand(connection, socks5Info);
        }
        public void CommandResponse(IConnection connection)
        {
            (ulong id, Memory<byte> data) = Socks5Info.Read(connection.ReceiveRequestWrap.Memory);
            socks5ClientListener.CommandResponse(id, (Socks5EnumResponseCommand)data.Span[0]);
        }

        public void Forward(IConnection connection)
        {
            Socks5Info socks5Info = new Socks5Info();
            socks5Info.DeBytes(connection.ReceiveRequestWrap.Memory);
            socks5ServerHandler.HndleForward(connection, socks5Info);
        }
        public void Response(IConnection connection)
        {
            (ulong id, Memory<byte> data) = Socks5Info.Read(connection.ReceiveRequestWrap.Memory);
            socks5ClientListener.Response(id, data);
        }
    }
}
