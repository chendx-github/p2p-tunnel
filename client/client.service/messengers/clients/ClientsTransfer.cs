using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.service.messengers.heart;
using client.service.messengers.punchHole;
using common.libs;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.messengers.clients
{
    public class ClientsTransfer : IClientsTransfer
    {
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly ClientsMessengerSender clientsMessengerSender;

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp,
            HeartMessengerSender heartMessengerSender, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender,
            WheelTimer<object> wheelTimer
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.heartMessengerSender = heartMessengerSender;

            punchHoleUdp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP));
            punchHoleUdp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, ServerType.UDP));
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });
            punchHoleUdp.OnStep4Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, ServerType.TCP));
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));

            this.punchHoleMessengerSender = punchHoleMessengerSender;
            //有人要求反向链接
            punchHoleMessengerSender.OnReverse.Sub(OnReverse);
            //本客户端注册状态
            registerState.OnRegisterStateChange.Sub(OnRegisterStateChange);

            this.clientsMessengerSender = clientsMessengerSender;
            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData.Sub(OnServerSendClients);

            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = Heart }, 5000, true);

            Task.Run(() =>
            {
                registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
            });
        }

        public void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        public void ConnectClient(ClientInfo info)
        {
            if (info.Id == registerState.ConnectId)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (info.UdpConnecting == false && info.UdpConnected == false)
                {
                    await ConnectUdp(info).ConfigureAwait(false);
                }
                if (info.TcpConnecting == false && info.TcpConnected == false)
                {
                    await ConnectTcp(info).ConfigureAwait(false);
                }
            });
        }
        public void ConnectReverse(ulong id)
        {
            punchHoleMessengerSender.SendReverse(id).ConfigureAwait(false);
        }
        public void Reset(ulong id)
        {
            punchHoleMessengerSender.SendReset(id).ConfigureAwait(false);
        }
        public void ConnectStop(ulong id)
        {
            punchHoleTcp.SendStep2Stop(id);
        }

        private async Task ConnectUdp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleUdp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = "udp",
                TryTimes = 5
            }).ConfigureAwait(false);

            if (!result.State)
            {
                if (registerState.RemoteInfo.Relay)
                {
                    IConnection connection = registerState.UdpConnection.Clone();
                    connection.Relay = registerState.RemoteInfo.Relay;
                    clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                }
                else
                {
                    Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                    clientInfoCaching.Offline(info.Id, ServerType.UDP);
                }
            }
        }
        private async Task ConnectTcp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.TCP);
            var result = await punchHoleTcp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = "tcp",
                TryTimes = 10
            }).ConfigureAwait(false);
            if (!result.State)
            {
                if (registerState.RemoteInfo.Relay)
                {
                    IConnection connection = registerState.TcpConnection.Clone();
                    connection.Relay = registerState.RemoteInfo.Relay;
                    clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                }
                else
                {
                    Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                    clientInfoCaching.Offline(info.Id, ServerType.TCP);
                }
            }
        }

        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }

        private void OnRegisterStateChange(bool state)
        {
            if (!state)
            {
                firstClients.Reset();
                foreach (ClientInfo client in clientInfoCaching.All())
                {
                    clientInfoCaching.Remove(client.Id);
                    if (client.UdpConnecting)
                    {
                        punchHoleTcp.SendStep2Stop(client.Id);
                    }
                }
            }
        }
        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {
                if (!registerState.LocalInfo.TcpConnected || clients.Clients == null)
                {
                    return;
                }

                IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.Id);
                //下线了的
                IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                foreach (ulong offid in offlines)
                {
                    clientInfoCaching.Offline(offid);
                    clientInfoCaching.Remove(offid);
                }
                //新上线的
                IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                IEnumerable<ClientsClientInfo> upLineClients = clients.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId);

                foreach (ClientsClientInfo item in upLineClients)
                {
                    ClientInfo client = new ClientInfo
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Mac = item.Mac
                    };
                    clientInfoCaching.Add(client);
                    if (firstClients.Get())
                    {
                        ConnectClient(client);
                    }
                }

                firstClients.Reverse();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private async void Heart(WheelTimerTimeout<object> timeout)
        {
            long time = DateTimeHelper.GetTimeStamp();
            foreach (ClientInfo client in clientInfoCaching.All())
            {
                if (client.UdpConnection != null)
                {
                    if (client.UdpConnection.IsTimeout(time))
                    {
                        clientInfoCaching.Offline(client.Id);
                    }
                    else if (client.UdpConnected && client.UdpConnection.IsNeedHeart(time))
                    {
                        await heartMessengerSender.Heart(client.UdpConnection).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
