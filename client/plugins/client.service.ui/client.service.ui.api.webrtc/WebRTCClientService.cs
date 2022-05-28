using client.messengers.clients;
using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using common.server.model;
using server.service.webrtc.models;
using System.Threading.Tasks;

namespace client.service.ui.api.webrtc
{
    public class WebRTCClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;


        public WebRTCClientService(MessengerSender messengerSender, RegisterStateInfo registerState, IClientInfoCaching clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
        }

        public async Task<bool> Execute(ClientServiceParamsInfo arg)
        {
            WebRTCParamsInfo model = arg.Content.DeJson<WebRTCParamsInfo>();

            if(model.ToId == 0)
            {
                await arg.Connection.Send(new ClientServiceResponseInfo
                {
                    Code = 0,
                    Content = new WebRTCConnectionInfo
                    {
                        FromId = 0,
                        ToId = model.ToId,
                        Data = model.Data
                    },
                    RequestId = 0,
                    Path = arg.Path
                }.ToJson());
                return true;
            }

            if (registerState.TcpConnection != null)
            {
                IConnection connection = registerState.TcpConnection;
                if (clientInfoCaching.Get(model.ToId, out ClientInfo client))
                {
                    if (client.TcpConnected)
                    {
                        connection = client.TcpConnection;
                    }
                    else if (client.UdpConnected)
                    {
                        connection = client.UdpConnection;
                    }
                }

                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestParamsInfo<WebRTCConnectionInfo>
                {
                    Data = new WebRTCConnectionInfo
                    {
                        FromId = registerState.TcpConnection.ConnectId,
                        ToId = model.ToId,
                        Data = model.Data
                    },
                    Path = "webrtc/execute",
                    Connection = connection,
                    Timeout = 15000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return true;
                }
                else
                {
                    arg.SetCode(-1, resp.Code.GetDesc((byte)resp.Code));
                }
            }
            else
            {
                arg.SetCode(-1, "未注册");
            }

            return false;
        }
    }

    public class WebRTCParamsInfo
    {
        public ulong ToId { get; set; } = 0;
        public string Data { get; set; } = string.Empty;
    }
}
