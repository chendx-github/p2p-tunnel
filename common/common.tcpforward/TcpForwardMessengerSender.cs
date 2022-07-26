using common.libs;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace common.tcpforward
{
    public class TcpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public TcpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public SimpleSubPushHandler<TcpForwardInfo> OnRequestHandler { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public async Task SendRequest(TcpForwardInfo arg)
        {
            await messengerSender.SendOnly(new MessageRequestParamsInfo<byte[]>
            {
                Path = "TcpForward/Request",
                Connection = arg.Connection,
                Data = arg.ToBytes()
            }).ConfigureAwait(false);
        }
        public void OnRequest(TcpForwardInfo data)
        {
            OnRequestHandler.Push(data);
        }

        public SimpleSubPushHandler<TcpForwardInfo> OnResponseHandler { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public async Task SendResponse(TcpForwardInfo arg)
        {
            await messengerSender.SendOnly(new MessageRequestParamsInfo<byte[]>
            {
                Path = "TcpForward/Response",
                Connection = arg.Connection,
                Data = arg.ToBytes()
            }).ConfigureAwait(false);
        }
        public void OnResponse(TcpForwardInfo data)
        {
            OnResponseHandler.Push(data) ;
        }

        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
            {
                Path = "TcpForward/GetPorts",
                Connection = Connection,
                Data = Helper.EmptyArray
            }).ConfigureAwait(false);
        }

        public async Task<MessageResponeInfo> UnRegister(IConnection Connection, TcpForwardUnRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<TcpForwardUnRegisterParamsInfo>
            {
                Path = "TcpForward/UnRegister",
                Connection = Connection,
                Data = data
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> Register(IConnection Connection, TcpForwardRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<TcpForwardRegisterParamsInfo>
            {
                Path = "TcpForward/Register",
                Connection = Connection,
                Data = data
            }).ConfigureAwait(false);
        }
    }
}
