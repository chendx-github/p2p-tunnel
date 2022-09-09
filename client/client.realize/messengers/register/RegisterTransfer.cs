using client.messengers.clients;
using client.messengers.register;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.realize.messengers.register
{
    public class RegisterTransfer : IRegisterTransfer
    {
        private readonly RegisterMessengerSender registerMessageHelper;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly CryptoSwap cryptoSwap;
        private int lockObject = new();
        IClientsTransfer clientsTransfer;
        public RegisterTransfer(
            RegisterMessengerSender registerMessageHelper, HeartMessengerSender heartMessengerSender,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState,
            CryptoSwap cryptoSwap, WheelTimer<object> wheelTimer, IClientsTransfer clientsTransfer
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartMessengerSender = heartMessengerSender;
            this.cryptoSwap = cryptoSwap;
            this.clientsTransfer = clientsTransfer;
            //发送心跳 timer
            //wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = Heart }, 1000, true);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => Exit();

            tcpServer.OnDisconnect.Sub((connection) =>
            {
                if (registerState.TcpConnection != connection)
                {
                    return;
                }
                if (registerState.LocalInfo.IsConnecting)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref lockObject, 1, 0) == 0)
                {
                    if (registerState.TcpConnection != connection)
                    {
                        return;
                    }
                    if (registerState.LocalInfo.IsConnecting)
                    {
                        return;
                    }
                    Logger.Instance.DebugDebug($"tcp掉线");
                    Interlocked.Exchange(ref lockObject, 0);
                    //_ = Register(true).Result;
                }

            });
            udpServer.OnDisconnect.Sub((connection) =>
            {
                Logger.Instance.DebugDebug($"udp掉线事件");
                if (registerState.UdpConnection != connection)
                {
                    Logger.Instance.DebugDebug($"udp对象不匹配 {connection.ConnectId}");
                    //clientsTransfer.ConnectClient(connection.ConnectId);
                    //Logger.Instance.DebugDebug($"重连完成");
                    return;
                }
                if (registerState.LocalInfo.IsConnecting)
                {
                    Logger.Instance.DebugDebug($"udp连接中");
                    return;
                }

                if (Interlocked.CompareExchange(ref lockObject, 1, 0) == 0)
                {
                    if (registerState.UdpConnection != connection)
                    {
                        return;
                    }
                    if (registerState.LocalInfo.IsConnecting)
                    {
                        return;
                    }
                    Logger.Instance.DebugDebug($"udp掉线");
                    Register(true).ContinueWith((res) =>
                    {
                        Interlocked.Exchange(ref lockObject, 0);
                    });
                }

            });
        }

        public void Exit()
        {
            registerMessageHelper.Exit().Wait();
            registerState.Offline();
            udpServer.Stop();
            tcpServer.Stop();
            GCHelper.FlushMemory();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoReg">强行自动注册</param>
        /// <returns></returns>
        public async Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false)
        {
            CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };
            if (registerState.LocalInfo.IsConnecting)
            {
                success.ErrorMsg = "注册操作中...";
                return success;
            }

            return await Task.Run(async () =>
            {

                int interval = autoReg ? config.Client.AutoRegDelay : 0;

                for (int i = 0; i < config.Client.AutoRegTimes; i++)
                {
                    try
                    {
                        if (registerState.LocalInfo.IsConnecting)
                        {
                            success.ErrorMsg = "注册操作中...";
                            break;
                        }

                        //先退出
                        Exit();
                        Logger.Instance.Info($"开始注册");

                        registerState.LocalInfo.IsConnecting = true;

                        if (interval > 0)
                        {
                            await Task.Delay(interval);
                        }

                        IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                        registerState.LocalInfo.UdpPort = registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort();
                        // registerState.LocalInfo.UdpPort = NetworkHelper.GetRandomPort(new System.Collections.Generic.List<int> { registerState.LocalInfo.TcpPort });
                        registerState.LocalInfo.Mac = string.Empty;
                        //绑定udp
                        UdpBind(serverAddress);
                        if (registerState.UdpConnection == null)
                        {
                            success.ErrorMsg = "udp连接失败";
                        }
                        else
                        {
                            //绑定tcp
                            //TcpBind(serverAddress);

                            //交换密钥
                            if (config.Server.Encode)
                            {
                                await SwapCryptoTcp();
                            }

                            //注册
                            RegisterResult result = await GetRegisterResult();
                            //上线
                            config.Client.GroupId = result.Data.GroupId;
                            registerState.RemoteInfo.TimeoutDelay = result.Data.TimeoutDelay;
                            registerState.RemoteInfo.Relay = result.Data.Relay;
                            registerState.Online(result.Data.Id, result.Data.Ip, result.Data.UdpPort, result.Data.TcpPort);
                            //上线通知
                            await registerMessageHelper.Notify().ConfigureAwait(false);

                            success.ErrorMsg = "注册成功~";
                            success.Data = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        success.ErrorMsg = ex.Message;
                    }

                    if (!success.Data)
                    {
                        Logger.Instance.Error(success.ErrorMsg);
                        registerState.LocalInfo.IsConnecting = false;

                        if (config.Client.AutoReg || autoReg)
                        {
                            interval = config.Client.AutoRegInterval;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Logger.Instance.Debug(success.ErrorMsg);
                        break;
                    }
                }
                return success;
            });
        }
        private void UdpBind(IPAddress serverAddress)
        {
            //UDP 开始监听
            udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.BindIp);
            registerState.UdpConnection = udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
        }
        private void TcpBind(IPAddress serverAddress)
        {
            //TCP 本地开始监听
            tcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);
            //TCP 连接服务器
            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive();
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
            if (config.Client.UseMac)
            {
                //registerState.LocalInfo.Mac = NetworkHelper.GetMacAddress(registerState.LocalInfo.LocalIp.ToString());
            }
            registerState.TcpConnection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);
        }
        private async Task SwapCryptoTcp()
        {
            ICrypto crypto = await cryptoSwap.Swap(registerState.TcpConnection, registerState.UdpConnection, config.Server.EncodePassword);
            if (crypto == null)
            {
                throw new Exception("注册交换密钥失败，如果客户端设置了密钥，则服务器必须设置相同的密钥，如果服务器未设置密钥，则客户端必须留空");
            }
            if (registerState.TcpConnection != null)
                registerState.TcpConnection.EncodeEnable(crypto);
            registerState.UdpConnection.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(registerState.UdpConnection);
#endif
        }
        private async Task<RegisterResult> GetRegisterResult()
        {
            //注册
            RegisterResult result = await registerMessageHelper.Register(new RegisterParams
            {
                ClientName = config.Client.Name,
                GroupId = config.Client.GroupId,
                LocalUdpPort = registerState.LocalInfo.UdpPort,
                LocalTcpPort = registerState.LocalInfo.TcpPort,
                Mac = registerState.LocalInfo.Mac,
                LocalIps = new IPAddress[] { config.Client.LoopbackIp, registerState.LocalInfo.LocalIp },
                Key = config.Client.Key,
                Timeout = 15 * 1000
            }).ConfigureAwait(false);
            if (result.NetState.Code != MessageResponeCodes.OK)
            {
                throw new Exception($"注册失败，网络问题:{result.NetState.Code.GetDesc((byte)result.NetState.Code)}");
            }
            if (result.Data.Code != RegisterResultInfo.RegisterResultInfoCodes.OK)
            {
                throw new Exception($"注册失败:{result.Data.Code.GetDesc((byte)result.Data.Code)}");
            }
            return result;
        }
    }
}
