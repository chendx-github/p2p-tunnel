﻿using client.messengers.clients;
using client.messengers.register;
using client.realize.messengers.crypto;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.realize.messengers.register
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterTransfer : IRegisterTransfer
    {
        private readonly RegisterMessengerSender registerMessageHelper;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly CryptoSwap cryptoSwap;
        private readonly IIPv6AddressRequest iPv6AddressRequest;
        private CancellationTokenSource cancellationToken = null;
        private int lockObject = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registerMessageHelper"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="tcpServer"></param>
        /// <param name="udpServer"></param>
        /// <param name="config"></param>
        /// <param name="registerState"></param>
        /// <param name="cryptoSwap"></param>
        /// <param name="iPv6AddressRequest"></param>
        public RegisterTransfer(
            RegisterMessengerSender registerMessageHelper, IClientInfoCaching clientInfoCaching,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState,
            CryptoSwap cryptoSwap, IIPv6AddressRequest iPv6AddressRequest
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.cryptoSwap = cryptoSwap;
            this.iPv6AddressRequest = iPv6AddressRequest;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => Exit();

            tcpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.UdpConnection));
        }
        private void Disconnect(IConnection connection, IConnection regConnection)
        {
            if (regConnection == null) return;
            if (ReferenceEquals(regConnection, connection) == false)
            {
                return;
            }
            if (registerState.LocalInfo.IsConnecting)
            {
                return;
            }

            Logger.Instance.Error($"{connection.ServerType} 断开~~~~");
            if (Interlocked.CompareExchange(ref lockObject, 1, 0) == 0)
            {
                Register(true).ContinueWith((result) =>
                {
                    Interlocked.Exchange(ref lockObject, 0);
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Exit()
        {
            if (cancellationToken != null && cancellationToken.IsCancellationRequested == false)
            {
                cancellationToken.Cancel();
            }
            Exit1();
        }
        private void Exit1()
        {
            //registerMessageHelper.Exit().Wait();
            registerState.Offline();
            udpServer.Stop();
            tcpServer.Stop();
            GCHelper.FlushMemory();
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="autoReg">强行自动注册</param>
        /// <returns></returns>
        public async Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false)
        {
            cancellationToken = new CancellationTokenSource();
            CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };
            if (registerState.LocalInfo.IsConnecting)
            {
                success.ErrorMsg = "注册操作中...";
                return success;
            }
            if (config.Client.UseUdp == false && config.Client.UseTcp == false)
            {
                success.ErrorMsg = "udp tcp至少要启用一种...";
                return success;
            }

            return await Task.Run(async () =>
            {
                int interval = autoReg ? config.Client.AutoRegDelay : 0;
                for (int i = 0; i < config.Client.AutoRegTimes; i++)
                {
                    bool isex = false;
                    try
                    {
                        if (registerState.LocalInfo.IsConnecting)
                        {
                            success.ErrorMsg = "注册操作中...";
                            Logger.Instance.Error(success.ErrorMsg);
                            break;
                        }

                        //先退出
                        Exit1();
                        Logger.Instance.Info($"开始注册");

                        registerState.LocalInfo.IsConnecting = true;
                        if (interval > 0)
                        {
                            await Task.Delay(interval, cancellationToken.Token);
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            success.ErrorMsg = "已取消...";
                            Logger.Instance.Error(success.ErrorMsg);
                            break;
                        }

                        IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                        config.Client.UseIpv6 = serverAddress.AddressFamily == AddressFamily.InterNetworkV6;
                        registerState.LocalInfo.UdpPort = registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort();
                        registerState.OnRegisterBind.Push(true);

                        if (config.Client.UseUdp)
                        {
                            //绑定udp
                            await UdpBind(serverAddress);
                            if (registerState.UdpConnection == null)
                            {
                                registerState.LocalInfo.IsConnecting = false;
                                success.ErrorMsg = "udp连接失败";
                                Logger.Instance.Error(success.ErrorMsg);
                                continue;
                            }
                        }
                        if (config.Client.UseTcp)
                        {
                            //绑定tcp
                            TcpBind(serverAddress);
                        }

                        //交换密钥
                        if (config.Server.Encode)
                        {
                            await SwapCryptoTcp();
                        }

                        //注册
                        RegisterResult result = await GetRegisterResult();
                        config.Client.ShortId = result.Data.ShortId;
                        config.Client.GroupId = result.Data.GroupId;
                        registerState.RemoteInfo.Relay = result.Data.Relay;
                        registerState.Online(result.Data.Id, result.Data.Ip, result.Data.UdpPort, result.Data.TcpPort);
                        //上线通知
                        await registerMessageHelper.Notify().ConfigureAwait(false);

                        success.ErrorMsg = "注册成功~";
                        success.Data = true;

                        Logger.Instance.Debug(success.ErrorMsg);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        Logger.Instance.Error(ex.Message);
                        success.ErrorMsg = ex.Message;
                        isex = true;
                    }

                    registerState.LocalInfo.IsConnecting = false;
                    if ((config.Client.AutoReg || autoReg) && isex == false)
                    {
                        interval = config.Client.AutoRegInterval;
                    }
                    else
                    {
                        break;
                    }
                }
                return success;
            });
        }
        private async Task UdpBind(IPAddress serverAddress)
        {
            //UDP 开始监听
            udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.TimeoutDelay);
            if (udpServer is UdpServer udp)
            {
                udp.SetSpeedLimit(config.Client.UdpUploadSpeedLimit);
            }
            registerState.UdpConnection = await udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
        }
        private void TcpBind(IPAddress serverAddress)
        {
            //TCP 本地开始监听
            tcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tcpServer.Start(registerState.LocalInfo.TcpPort);

            //TCP 连接服务器
            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
            tcpSocket.IPv6Only(config.Client.BindIp.AddressFamily, false);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
            registerState.TcpConnection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);
        }
        private async Task SwapCryptoTcp()
        {
            ICrypto crypto = await cryptoSwap.Swap(registerState.TcpConnection, registerState.UdpConnection, config.Server.EncodePassword);
            if (crypto == null)
            {
                throw new Exception("注册交换密钥失败，如果客户端设置了密钥，则服务器必须设置相同的密钥，如果服务器未设置密钥，则客户端必须留空");
            }

            registerState.TcpConnection?.EncodeEnable(crypto);
            registerState.UdpConnection?.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(registerState.OnlineConnection);
#endif
        }
        private async Task<RegisterResult> GetRegisterResult()
        {
            IPAddress[] localIps = new IPAddress[] { config.Client.LoopbackIp, registerState.LocalInfo.LocalIp };

            registerState.LocalInfo.Ipv6s = iPv6AddressRequest.GetIPV6();

            localIps = localIps.Concat(registerState.LocalInfo.Ipv6s).ToArray();

            //注册
            RegisterResult result = await registerMessageHelper.Register(new RegisterParams
            {
                ShortId = config.Client.ShortId,
                ClientName = config.Client.Name,
                GroupId = config.Client.GroupId,
                LocalUdpPort = registerState.LocalInfo.UdpPort,
                LocalTcpPort = registerState.LocalInfo.TcpPort,
                LocalIps = localIps,
                Timeout = 15 * 1000,
                ClientAccess = config.Client.GetAccess()
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
