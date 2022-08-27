using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace common.udpforward
{
    public class UdpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public UdpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public SimpleSubPushHandler<UdpForwardInfo> OnRequestHandler { get; } = new SimpleSubPushHandler<UdpForwardInfo>();
        public async Task SendRequest(UdpForwardInfo arg)
        {
            var res = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "UdpForward/Request",
                Connection = arg.Connection,
                Content = arg.ToBytes()
            }).ConfigureAwait(false);
            await res;
        }
        public void OnRequest(UdpForwardInfo data)
        {
            OnRequestHandler.Push(data);
        }

        public SimpleSubPushHandler<UdpForwardInfo> OnResponseHandler { get; } = new SimpleSubPushHandler<UdpForwardInfo>();
        public async Task SendResponse(UdpForwardInfo arg)
        {
            var res = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "UdpForward/Response",
                Connection = arg.Connection.FromConnection,
                Content = arg.ToBytes()
            }).ConfigureAwait(false);
            await res;
        }
        public void OnResponse(UdpForwardInfo data)
        {
            OnResponseHandler.Push(data);
        }

        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "UdpForward/GetPorts",
                Connection = Connection,
                Content = Helper.EmptyArray
            }).ConfigureAwait(false);
        }

        public async Task<MessageResponeInfo> UnRegister(IConnection Connection, ushort port)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "UdpForward/UnRegister",
                Connection = Connection,
                Content = port.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> Register(IConnection Connection, UdpForwardRegisterParamsInfo param)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "UdpForward/Register",
                Connection = Connection,
                Content = param.ToBytes(),
            }).ConfigureAwait(false);
        }
    }
}
