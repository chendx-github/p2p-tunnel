using client.messengers.register;
using client.service.messengers.crypto;
using client.service.messengers.heart;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.messengers.register
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

        public RegisterTransfer(
            RegisterMessengerSender registerMessageHelper, HeartMessengerSender heartMessengerSender,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState,
            CryptoSwap cryptoSwap
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartMessengerSender = heartMessengerSender;
            this.cryptoSwap = cryptoSwap;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ = Exit();
            Console.CancelKeyPress += (s, e) => _ = Exit();
            tcpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                if (registerState.TcpConnection != null && connection.ConnectId == registerState.TcpConnection.ConnectId)
                {
                    Task.Run(async () =>
                    {
                        await ExitAndAutoReg().ConfigureAwait(false);
                    });
                }
            });
        }

        public async Task AutoReg()
        {
            if (config.Client.AutoReg && !registerState.LocalInfo.IsConnecting)
            {
                Logger.Instance.Info("开始自动注册");
                while (true)
                {
                    CommonTaskResponseInfo<bool> result = await Register().ConfigureAwait(false);
                    if (result.Data == true)
                    {
                        break;
                    }
                    else
                    {
                        Logger.Instance.Error(result.ErrorMsg);
                    }
                    await Task.Delay(5000).ConfigureAwait(false);
                }
                Logger.Instance.Warning("已自动注册");
            }
        }
        public async Task Exit()
        {
            await registerMessageHelper.Exit().ConfigureAwait(false);
            udpServer.Stop();
            tcpServer.Stop();
            registerState.Offline();
            GCHelper.FlushMemory();
        }
        private async Task ExitAndAutoReg()
        {
            await Exit();
            await AutoReg();
        }

        public async Task<CommonTaskResponseInfo<bool>> Register()
        {
            try
            {
                await Exit().ConfigureAwait(false);

                IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                registerState.LocalInfo.IsConnecting = true;
                registerState.LocalInfo.UdpPort = NetworkHelper.GetRandomPort();
                registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort(new List<int> { registerState.LocalInfo.UdpPort });
                registerState.LocalInfo.Mac = string.Empty;

                UdpBind(serverAddress);
                if (registerState.UdpConnection == null)
                {
                    await Exit().ConfigureAwait(false);
                    return new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = "udp连接失败" };
                }
                TcpBind(serverAddress);

                //交换密钥
                if (config.Server.Encode)
                {
                    await SwapCryptoTcp();
                }

                //注册
                RegisterResult result = await GetRegisterResult();
                //上线
                config.Client.GroupId = result.Data.GroupId;
                registerState.RemoteInfo.Relay = result.Data.Relay;
                registerState.Online(result.Data.Id, result.Data.Ip, result.Data.Port, result.Data.TcpPort);
                //上线通知
                await OnlineNotify();

                return new CommonTaskResponseInfo<bool> { Data = true, ErrorMsg = string.Empty };
            }
            catch (SocketException sex)
            {
                if (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return await Register();
                }
                else
                {
                    await Exit().ConfigureAwait(false);
                    Logger.Instance.DebugError(sex);
                    return new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = sex.Message };
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                await Exit().ConfigureAwait(false);
                return new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = ex.Message };
            }
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
#if DEBUG
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address.ToString();
#endif
            if (config.Client.UseMac)
            {
                registerState.LocalInfo.Mac = NetworkHelper.GetMacAddress(registerState.LocalInfo.LocalIp);
            }
            registerState.TcpConnection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);
        }
        private async Task SwapCryptoTcp()
        {
            ICrypto crypto = await cryptoSwap.Swap(registerState.TcpConnection, registerState.UdpConnection);
            if (crypto == null)
            {
                throw new Exception("注册交换密钥失败");
            }
            registerState.TcpConnection.EncodeEnable(crypto);
            registerState.UdpConnection.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(registerState.TcpConnection);
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
                LocalIps = string.Join(Helper.SeparatorString, new List<string> { config.Client.LoopbackIp.ToString(), registerState.LocalInfo.LocalIp }),
                Key = config.Client.Key,
                Timeout = 5 * 1000
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
        private async Task OnlineNotify()
        {
            if (await registerMessageHelper.Notify().ConfigureAwait(false))
            {
                Logger.Instance.Warning("已通知上线信息");
            }
            else
            {
                Logger.Instance.Error("通知上线信息失败");
            }
        }
    }
}
