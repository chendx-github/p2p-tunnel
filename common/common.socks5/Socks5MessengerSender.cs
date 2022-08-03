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
        public void Response(Socks5Info data, IConnection connection)
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "socks5/response",
                Connection = connection,
                Content = data.ToBytes()
            }).ConfigureAwait(false);
        }
        public void ResponseClose(ulong id, IConnection connection)
        {
            Response(new Socks5Info { Id = id, Data = Helper.EmptyArray, Socks5Step = Socks5EnumStep.Forward }, connection);
        }
        public void RequestClose(ulong id, IConnection connection)
        {
            Request(new Socks5Info { Id = id, Data = Helper.EmptyArray, Socks5Step = Socks5EnumStep.Forward }, connection);
        }
    }
}
