using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.register;
using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.messengers.punchHole.tcp.nutssb
{
    public class PunchHoleTcpNutssBMessengerSender : IPunchHoleTcp
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly ITcpServer tcpServer;
        private readonly RegisterStateInfo registerState;
        private readonly Config config;

        public PunchHoleTcpNutssBMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, ITcpServer tcpServer,
            RegisterStateInfo registerState, Config config)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.config = config;
        }

        private IConnection TcpServer => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;

        public int ClientTcpPort => registerState.LocalInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel + 2;
#if DEBUG
        private bool UseLocalPort = false;
#else
        private bool UseLocalPort = true;
#endif
        private bool UseGuesstPort = false;

        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectTcpCache = new();


        public SimpleSubPushHandler<ConnectParams> OnSendHandler => new SimpleSubPushHandler<ConnectParams>();
        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();
            connectTcpCache.TryAdd(param.Id, new ConnectCacheModel
            {
                TryTimes = param.TryTimes,
                Tcs = tcs,
                TunnelName = param.TunnelName,
            });

            int port = 0;
            if (UseGuesstPort)
            {
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
            }
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                TunnelName = param.TunnelName,
                Connection = TcpServer,
                ToId = param.Id,
                GuessPort = port,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_1, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1Params>();
        public async Task OnStep1(OnStep1Params arg)
        {
            if (arg.Data.IsDefault)
            {
                OnStep1Handler.Push(arg);
            }

            List<Tuple<string, int>> ips = arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, arg.Data.LocalPort)).ToList();
            ips.Add(new Tuple<string, int>(arg.Data.Ip, arg.Data.Port));

            foreach (Tuple<string, int> ip in ips)
            {
                IPEndPoint target = new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2);
                using Socket targetSocket = new(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    _ = targetSocket.ConnectAsync(target);
                }
                catch (Exception)
                {
                }
                targetSocket.SafeClose();
            }

            int port = 0;
            if (arg.Data.GuessPort > 0)
            {
                int bindPort = NetworkHelper.GetRandomPort();
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
                int startPort = arg.Data.Port;
                int endPort = arg.Data.Port;
                if (arg.Data.GuessPort > 0)
                {
                    startPort = arg.Data.GuessPort;
                    endPort = startPort + 20;
                }
                if (endPort > 65535)
                {
                    endPort = 65535;
                }
                for (int i = startPort; i <= endPort; i++)
                {
                    IPEndPoint localEndPoint = new IPEndPoint(config.Client.BindIp, ClientTcpPort);
                    //var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    //socket.ReuseBind(localEndPoint);
                    //socket.Listen(int.MaxValue);

                    //_ = Task.Run(() =>
                    //{
                    //    while (true)
                    //    {
                    //        var client = socket.Accept();
                    //        Console.WriteLine($"收到连接：{client.RemoteEndPoint}");
                    //    }
                    //});

                    IPEndPoint target = new IPEndPoint(IPAddress.Parse(arg.Data.Ip), i);
                    using Socket targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.ReuseBind(localEndPoint);
                    _ = targetSocket.ConnectAsync(target);

                    targetSocket.SafeClose();
                }
            }

            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                GuessPort = port,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            OnStep2Handler.Push(arg);

            List<Tuple<string, int>> ips = new List<Tuple<string, int>>();
            if (UseLocalPort && registerState.RemoteInfo.Ip == arg.Data.Ip)
            {
                ips = arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, arg.Data.LocalPort)).ToList();
            }

            ips.Add(new Tuple<string, int>(arg.Data.Ip, arg.Data.Port));
            if (!connectTcpCache.TryGetValue(arg.RawData.FromId, out ConnectCacheModel cache))
            {
                return;
            }


            bool success = false;
            int length = cache.TryTimes, index = 0, interval = 0, port = 0;
            while (length > 0)
            {
                if (cache.Canceled)
                {
                    break;
                }
                if (interval > 0)
                {
                    await Task.Delay(interval).ConfigureAwait(false);
                    interval = 0;
                }

                Tuple<string, int> ip = index >= ips.Count ? ips[^1] : ips[index];
                if (port == 0)
                {
                    port = ip.Item2;
                }
                IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(ip.Item1), port);
                Socket targetSocket = new Socket(targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.KeepAlive();
                    targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    IAsyncResult result = targetSocket.BeginConnect(targetEndpoint, null, null);
                    result.AsyncWaitHandle.WaitOne(2000, false);

                    if (result.IsCompleted)
                    {
                        if (cache.Canceled)
                        {
                            targetSocket.SafeClose();
                            break;
                        }
                        targetSocket.EndConnect(result);

                        if (arg.Data.IsDefault)
                        {
                            IConnection connection = tcpServer.BindReceive(targetSocket, bufferSize: config.Client.TcpBufferSize);
                            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep3Info>
                            {
                                TunnelName = arg.RawData.TunnelName,
                                Connection = connection,
                                Data = new PunchHoleStep3Info
                                {
                                    FromId = ConnectId,
                                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_3,
                                    PunchType = PunchHoleTypes.TCP_NUTSSB
                                }
                            }).ConfigureAwait(false);
                        }
                        else
                        {
                            if (connectTcpCache.TryRemove(arg.RawData.FromId, out _))
                            {
                                cache.Tcs.SetResult(new ConnectResultModel { State = true });
                            }
                        }
                        success = true;
                        break;
                    }
                    else
                    {
                        targetSocket.SafeClose();
                        interval = 300;
                        await SendStep2Retry(arg.RawData.FromId, arg.RawData.TunnelName).ConfigureAwait(false);
                        if (arg.Data.GuessPort > 0)
                        {
                            interval = 0;
                            port = arg.Data.GuessPort + index;
                        }
                        length--;
                    }
                }
                catch (SocketException ex)
                {
                    Logger.Instance.DebugError(ex);
                    targetSocket.SafeClose();
                    targetSocket = null;
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        interval = 2000;
                    }
                    else
                    {
                        interval = 100;
                        await SendStep2Retry(arg.RawData.FromId, arg.RawData.TunnelName).ConfigureAwait(false);
                        if (arg.Data.GuessPort > 0)
                        {
                            interval = 0;
                            port = arg.Data.GuessPort + index;
                        }
                        length--;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }

                index++;
            }
            if (!success)
            {
                await SendStep2Fail(arg).ConfigureAwait(false);
            }
        }

        private async Task SendStep2Retry(ulong toid, string tunnelName)
        {
            int port = 0;
            if (UseGuesstPort)
            {
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
            }
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2TryInfo>
            {
                TunnelName = tunnelName,
                Connection = TcpServer,
                ToId = toid,
                GuessPort = port,
                Data = new PunchHoleStep2TryInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }
        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; } = new SimpleSubPushHandler<OnStep2RetryParams>();
        public async Task OnStep2Retry(OnStep2RetryParams e)
        {
            OnStep2RetryHandler.Push(e);
            await Task.Run(() =>
            {
                int startPort = e.Data.Port;
                int endPort = e.Data.Port;
                if (e.Data.GuessPort > 0)
                {
                    startPort = e.Data.GuessPort;
                    endPort = startPort + 20;
                }
                if (endPort > 65535)
                {
                    endPort = 65535;
                }
                for (int i = startPort; i <= endPort; i++)
                {

                    IPEndPoint target = new IPEndPoint(IPAddress.Parse(e.Data.Ip), i);

                    using Socket targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    _ = targetSocket.ConnectAsync(target);

                    targetSocket.SafeClose();
                }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler => new SimpleSubPushHandler<ulong>();
        private async Task SendStep2Fail(OnStep2Params arg)
        {

            if (connectTcpCache.TryRemove(arg.RawData.FromId, out ConnectCacheModel cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "失败",
                        Type = ConnectFailType.ERROR
                    }
                });
            }
            if (arg.Data.IsDefault)
            {
                OnSendStep2FailHandler.Push(arg.RawData.FromId);
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2FailInfo>
                {
                    TunnelName = arg.RawData.TunnelName,
                    Connection = TcpServer,
                    ToId = arg.RawData.FromId,
                    Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
            }
        }
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailParams>();
        public async Task OnStep2Fail(OnStep2FailParams arg)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            OnStep2FailHandler.Push(arg);
        }
        public async Task SendStep2Stop(ulong toid)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2StopInfo>
                {
                    TunnelName = cache.TunnelName,
                    Connection = TcpServer,
                    ToId = toid,
                    Data = new PunchHoleStep2StopInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                Cancel(toid);
            }
        }
        public async Task OnStep2Stop(OnStep2StopParams e)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            Cancel(e.RawData.FromId);
        }

        private void Cancel(ulong id)
        {
            if (connectTcpCache.TryRemove(id, out ConnectCacheModel cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "取消",
                        Type = ConnectFailType.CANCEL
                    }
                });
            }
        }

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3Params>();
        public async Task OnStep3(OnStep3Params arg)
        {
            OnStep3Handler.Push(arg);
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = arg.Connection,
                Data = new PunchHoleStep4Info
                {
                    FromId = ConnectId,
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_4,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            });
        }

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4Params>();
        public async Task OnStep4(OnStep4Params arg)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            if (connectTcpCache.TryRemove(arg.Data.FromId, out ConnectCacheModel cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true });
            }
            OnStep4Handler.Push(arg);
        }

    }
}
