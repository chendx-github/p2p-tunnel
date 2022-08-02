using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;

        public PunchHoleMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<PunchHoleParamsInfo>();

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
                        {
                            if (!source.GetTunnel(model.TunnelName, out TunnelRegisterCacheInfo tunnel))
                            {
                                return false;
                            }

                            model.Data = new PunchHoleNotifyInfo
                            {
                                Ip = source.UdpConnection.Address.Address,
                                LocalIps = source.LocalIps,
                                LocalPort = tunnel.LocalPort,
                                Port = tunnel.Port,
                                IsDefault = tunnel.IsDefault,
                                GuessPort = model.GuessPort
                            }.ToBytes();
                        }

                        model.FromId = connection.ConnectId;
                        return await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Content = connection.ReceiveRequestWrap.Memory,
                            MemoryPath = connection.ReceiveRequestWrap.MemoryPath,
                            RequestId = connection.ReceiveRequestWrap.RequestId
                        }).ConfigureAwait(false);
                    }
                }
            }
            return false;
        }
    }
}
