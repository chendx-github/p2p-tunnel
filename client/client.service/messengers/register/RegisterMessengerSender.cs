using client.messengers.register;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static common.server.model.RegisterResultInfo;

namespace client.service.messengers.register
{
    public class RegisterMessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly Config config;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;


        public RegisterMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, Config config, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.config = config;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public async Task Exit()
        {
            MessageResponeInfo res = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
            {
                Connection = registerState.TcpConnection ?? registerState.UdpConnection,
                Data = Helper.EmptyArray,
                Path = "exit/Execute"
            }).ConfigureAwait(false);
        }
        public async Task<RegisterResult> Register(RegisterParams param)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestParamsInfo<RegisterParamsInfo>
            {
                Connection = registerState.UdpConnection,
                Path = "register/Execute",
                Data = new RegisterParamsInfo
                {
                    Id = registerState.ConnectId,
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    LocalIps = param.LocalIps,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort,
                    Key = param.Key
                },
                Timeout = param.Timeout,
            }).ConfigureAwait(false);
            if (result.Code != MessageResponeCodes.OK)
            {
                return new RegisterResult { NetState = result };
            }

            RegisterResultInfo res = result.Data.DeBytes<RegisterResultInfo>();
            if (res.Code != RegisterResultInfoCodes.OK)
            {
                return new RegisterResult { NetState = result, Data = res };
            }
            MessageResponeInfo tcpResult = await messengerSender.SendReply(new MessageRequestParamsInfo<RegisterParamsInfo>
            {
                Connection = registerState.TcpConnection,
                Path = "register/Execute",
                Data = new RegisterParamsInfo
                {
                    Id = res.Id,
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort,
                    Key = param.Key,
                },
                Timeout = param.Timeout,
            }).ConfigureAwait(false);

            if (tcpResult.Code != MessageResponeCodes.OK)
            {
                return new RegisterResult { NetState = tcpResult };
            }

            RegisterResultInfo tcpRes = tcpResult.Data.DeBytes<RegisterResultInfo>();
            return new RegisterResult { NetState = tcpResult, Data = tcpRes };
        }
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new MessageRequestParamsInfo<byte[]>
            {
                Connection = registerState.TcpConnection,
                Data = Helper.EmptyArray,
                Path = "register/notify"
            }).ConfigureAwait(false);
        }

        public async Task<int> GetGuessPort(ServerType serverType)
        {
            var connection = GetConnection(serverType);

            MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
            {
                Connection = connection,
                Path = "register/port",
                Data = Helper.EmptyArray,
            }).ConfigureAwait(false);

            //connection.Disponse();
            if (result.Code != MessageResponeCodes.OK)
            {
                return 0;
            }
            return result.Data.Span.ToInt32();
        }
        private IConnection GetConnection(ServerType serverType)
        {
            IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
            if (serverType == ServerType.TCP)
            {
                IPEndPoint endpoint = new IPEndPoint(serverAddress, config.Server.TcpPort);
                Socket tcpSocket = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                tcpSocket.KeepAlive();
                tcpSocket.Connect(endpoint);
                return tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);
            }
            else
            {
                IPEndPoint endpoint = new IPEndPoint(serverAddress, config.Server.UdpPort);
                return udpServer.CreateConnection(endpoint);
            }
        }

        public async Task<TunnelRegisterInfo> TunnelInfo(IConnection connection)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
            {
                Connection = connection,
                Data = Helper.EmptyArray,
                Path = "register/TunnelInfo"
            }).ConfigureAwait(false);
            if (result.Code != MessageResponeCodes.OK)
            {
                return new TunnelRegisterInfo { Code = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.UNKNOW };
            }

            return result.Data.DeBytes<TunnelRegisterInfo>();
        }
        public async Task<TunnelRegisterResultInfo> TunnelRegister(TunnelRegisterParams param)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new MessageRequestParamsInfo<TunnelRegisterParamsInfo>
            {
                Connection = param.Connection,
                Data = new TunnelRegisterParamsInfo
                {
                    LocalPort = param.LocalPort,
                    Port = param.Port,
                    TunnelName = param.TunnelName
                },
                Path = "register/tunnel"
            }).ConfigureAwait(false);
            if (result.Code != MessageResponeCodes.OK)
            {
                return new TunnelRegisterResultInfo { Code = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.UNKNOW };
            }

            return result.Data.DeBytes<TunnelRegisterResultInfo>();
        }
    }

    public class TunnelRegisterParams
    {
        public TunnelRegisterParams() { }

        public string TunnelName { get; set; } = string.Empty;
        public int LocalPort { get; set; } = 0;
        public int Port { get; set; } = 0;
        public IConnection Connection { get; set; }
    }

    public class RegisterParams
    {
        public string GroupId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        public string Mac { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;
        public string Key { get; set; } = string.Empty;

    }

    public class RegisterResult
    {
        public MessageResponeInfo NetState { get; set; }
        public RegisterResultInfo Data { get; set; }
    }
}
