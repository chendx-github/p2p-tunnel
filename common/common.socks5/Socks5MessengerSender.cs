using common.libs;
using common.server;
using common.server.model;

namespace common.socks5
{
    public class Socks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public bool Request(Socks5Info data, IConnection connection)
        {
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/request",
                Connection = connection,
                Content = data.ToBytes()
            }).Result;
        }
        public void RequestResponse(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/requestresponse",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }

        public bool Auth(Socks5Info data, IConnection connection)
        {
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/auth",
                Connection = connection,
                Content = data.ToBytes()
            }).Result;
        }
        public void AuthResponse(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/authresponse",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }

        public bool Command(Socks5Info data, IConnection connection)
        {
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/command",
                Connection = connection,
                Content = data.ToBytes()
            }).Result;
        }
        public void CommandResponse(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/commandresponse",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }

        public bool Forward(Socks5Info data, IConnection connection)
        {
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/forward",
                Connection = connection,
                Content = data.ToBytes()
            }).Result;
        }
        public bool ForwardUdp(Socks5Info data, IConnection connection)
        {
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/forwardudp",
                Connection = connection,
                Content = data.ToBytes()
            }).Result;
        }
        public void Response(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/response",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }
        public void ResponseUdp(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/responseudp",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }

        public void ResponseClose(ulong id, IConnection connection)
        {
            Response(new Socks5Info { Id = id, Data = Helper.EmptyArray }, connection);
        }
        public void RequestClose(ulong id, IConnection connection)
        {
            Forward(new Socks5Info { Id = id, Data = Helper.EmptyArray }, connection);
        }
    }
}
