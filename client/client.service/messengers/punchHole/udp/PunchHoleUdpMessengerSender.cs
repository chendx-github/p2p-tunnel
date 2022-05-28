using client.messengers.punchHole;
using client.messengers.punchHole.udp;
using client.messengers.register;
using common.libs;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.service.messengers.punchHole.udp
{
    public class PunchHoleUdpMessengerSender : IPunchHoleUdp
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly IUdpServer udpServer;

        public PunchHoleUdpMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, RegisterStateInfo registerState, IUdpServer udpServer)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.registerState = registerState;
            this.udpServer = udpServer;
        }

        private IConnection connection => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectCache = new();

        public SimpleSubPushHandler<ConnectParams> OnSendHandler => new SimpleSubPushHandler<ConnectParams>();
        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();

            connectCache.TryAdd(param.Id, new ConnectCacheModel
            {
                Tcs = tcs,
                TryTimes = param.TryTimes
            });

            while (true)
            {
                if (!connectCache.TryGetValue(param.Id, out ConnectCacheModel cache))
                {
                    break;
                }
                if (cache.TryTimes < 0)
                {
                    await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2FailInfo>
                    {
                        Connection = connection,
                        TunnelName = param.TunnelName,
                        ToId = param.Id,
                        Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleUdpSteps.STEP_2_Fail, PunchType = PunchHoleTypes.UDP }
                    }).ConfigureAwait(false);
                    connectCache.TryRemove(param.Id, out _);
                    cache.Tcs.SetResult(new ConnectResultModel
                    {
                        State = false,
                        Result = new ConnectFailModel
                        {
                            Msg = "UDP连接超时",
                            Type = ConnectFailType.TIMEOUT
                        }
                    });
                    break;
                }

                cache.TryTimes--;
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep1Info>
                {
                    Connection = connection,
                    TunnelName = param.TunnelName,
                    ToId = param.Id,
                    Data = new PunchHoleStep1Info { Step = (byte)PunchHoleUdpSteps.STEP_1, PunchType = PunchHoleTypes.UDP }
                }).ConfigureAwait(false);

                await Task.Delay(1000).ConfigureAwait(false);
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1Params>();
        public async Task OnStep1(OnStep1Params arg)
        {
            if (arg.Data.IsDefault)
            {
                OnStep1Handler.Push(arg);
            }
            foreach (var ip in arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0))
            {
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep21Info>
                {
                    Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(ip), arg.Data.Port)),
                    TunnelName = arg.RawData.TunnelName,
                    Data = new PunchHoleStep21Info { Step = (byte)PunchHoleUdpSteps.STEP_2_1, PunchType = PunchHoleTypes.UDP }
                }).ConfigureAwait(false);
            }

            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep21Info>
            {
                Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(arg.Data.Ip), arg.Data.Port)),
                TunnelName = arg.RawData.TunnelName,
                Data = new PunchHoleStep21Info { Step = (byte)PunchHoleUdpSteps.STEP_2_1, PunchType = PunchHoleTypes.UDP }
            }).ConfigureAwait(false);

            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                Connection = arg.Connection,
                TunnelName = arg.RawData.TunnelName,
                ToId = arg.RawData.FromId,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleUdpSteps.STEP_2, PunchType = PunchHoleTypes.UDP }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            OnStep2Handler.Push(arg);
            if (arg.Data.IsDefault)
            {
                List<Tuple<string, int>> ips = arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
               .Select(c => new Tuple<string, int>(c, arg.Data.LocalPort)).ToList();
                ips.Add(new Tuple<string, int>(arg.Data.Ip, arg.Data.Port));

                foreach (Tuple<string, int> ip in ips)
                {
                    await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep3Info>
                    {
                        Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2)),
                        TunnelName = arg.RawData.TunnelName,
                        Data = new PunchHoleStep3Info
                        {
                            FromId = ConnectId,
                            Step = (byte)PunchHoleUdpSteps.STEP_3,
                            PunchType = PunchHoleTypes.UDP
                        }
                    }).ConfigureAwait(false);
                }
            }
            else
            {
                if (connectCache.TryRemove(arg.RawData.FromId, out ConnectCacheModel cache))
                {
                    cache.Tcs.SetResult(new ConnectResultModel { State = true });
                }
            }
        }

        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailParams>();
        public void OnStep2Fail(OnStep2FailParams arg)
        {
            OnStep2FailHandler.Push(arg);
        }

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3Params>();
        public async Task OnStep3(OnStep3Params arg)
        {
            OnStep3Handler.Push(arg);

            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                Connection = arg.Connection,
                TunnelName = arg.RawData.TunnelName,
                Data = new PunchHoleStep4Info
                {
                    FromId = ConnectId,
                    Step = (byte)PunchHoleUdpSteps.STEP_4,
                    PunchType = PunchHoleTypes.UDP
                }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4Params>();
        public void OnStep4(OnStep4Params arg)
        {
            if (connectCache.TryRemove(arg.Data.FromId, out ConnectCacheModel cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true });
            }
            OnStep4Handler.Push(arg);
        }
    }
}