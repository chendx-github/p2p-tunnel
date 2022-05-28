using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.service.manager.models;
using System.Threading.Tasks;

namespace client.service.ui.api.manager
{
    public class CounterClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;


        public CounterClientService(MessengerSender messengerSender, RegisterStateInfo registerState)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
        }

        public async Task<CommonResponseInfo<CounterResultInfo>> Info(ClientServiceParamsInfo arg)
        {
            if (registerState.TcpConnection != null)
            {
                var resp = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
                {
                    Data = Helper.EmptyArray,
                    Path = "Counter/Info",
                    Connection = registerState.TcpConnection,
                    Timeout = 15000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return resp.Data.DeBytes<CommonResponseInfo<CounterResultInfo>>();
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
            return null;
        }
    }
}
