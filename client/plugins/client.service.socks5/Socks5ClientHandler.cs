using client.messengers.clients;
using client.messengers.register;
using common.libs;
using common.libs.extends;
using common.server;
using common.socks5;
using System;
using System.Collections.Generic;

namespace client.service.socks5
{
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        private readonly Socks5MessengerSender socks5MessengerSender;
        private readonly Socks5ClientListener socks5ClientListener;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly common.socks5.Config config;
        private IConnection connection;
        private IClientInfoCaching clientInfoCaching;
        private readonly Dictionary<Socks5EnumStep, Func<Socks5Info, bool>> handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, bool>>();
        private readonly Dictionary<Socks5EnumStep, Action<Socks5Info>> buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();

        public Socks5ClientHandler(Socks5MessengerSender socks5MessengerSender, RegisterStateInfo registerStateInfo, common.socks5.Config config, IClientInfoCaching clientInfoCaching, Socks5ClientListener socks5ClientListener)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.registerStateInfo = registerStateInfo;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.socks5ClientListener = socks5ClientListener;

            socks5ClientListener.OnData = OnData;
            socks5ClientListener.OnClose = OnClose;

            handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, bool>>
            {
                { Socks5EnumStep.Request,HandleRequest},
                { Socks5EnumStep.Auth,HandleAuth},
                { Socks5EnumStep.Command,HandleCommand},
                { Socks5EnumStep.Forward,HndleForward},
                { Socks5EnumStep.ForwardUdp,HndleForwardUdp},
            };
            buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>> {
                {Socks5EnumStep.Request, RequestResponseData},
                {Socks5EnumStep.Auth, AuthResponseData},
                {Socks5EnumStep.Command, CommandResponseData},
                {Socks5EnumStep.Forward, ForwardResponseData},
                {Socks5EnumStep.ForwardUdp, ForwardUdpResponseData},
            };
        }
        public void InputData(IConnection connection)
        {
            Socks5Info info = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);
            if (info.Data.Length == 0)
            {
                socks5ClientListener.Close(info.Id);
            }
            else
            {
                if (buildHandles.TryGetValue(info.Socks5Step, out Action<Socks5Info> func))
                {
                    func(info);
                    socks5ClientListener.Response(info);
                }
            }
        }

        private bool OnData(Socks5Info info)
        {
            if (handles.TryGetValue(info.Socks5Step, out Func<Socks5Info, bool> func))
            {
                if (info.Socks5Step == Socks5EnumStep.Auth)
                {
                    info.Version = info.Data.Span[0];
                }

                return func(info);
            }
            return false;
        }
        private void OnClose(ulong id)
        {
            GetConnection();
            socks5MessengerSender.RequestClose(id, connection);
        }

        private void RequestResponseData(Socks5Info info)
        {
            Socks5EnumAuthType type = (Socks5EnumAuthType)info.Data.Span[0];
            if (type == Socks5EnumAuthType.NotSupported)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                if (type == Socks5EnumAuthType.NoAuth)
                {
                    info.Socks5Step = Socks5EnumStep.Command;
                }
                else
                {
                    info.Socks5Step = Socks5EnumStep.Auth;
                }
                info.Data = new byte[] { socks5ClientListener.Version, (byte)type };
            }
        }
        private void AuthResponseData(Socks5Info info)
        {
            Socks5EnumAuthState type = (Socks5EnumAuthState)info.Data.Span[0];
            if (type != Socks5EnumAuthState.Success)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                if (type == Socks5EnumAuthState.Success)
                {
                    info.Socks5Step = Socks5EnumStep.Command;
                }
                info.Data = new byte[] { info.Version, (byte)type };
            }
        }
        private void CommandResponseData(Socks5Info info)
        {
            Socks5EnumResponseCommand type = (Socks5EnumResponseCommand)info.Data.Span[0];
            if (type != Socks5EnumResponseCommand.ConnecSuccess)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                if (type == Socks5EnumResponseCommand.ConnecSuccess)
                {
                    info.Socks5Step = Socks5EnumStep.Forward;
                }
                info.Data = Socks5Parser.MakeConnectResponse(socks5ClientListener.DistEndpoint, (byte)type);
            }
        }
        private void ForwardResponseData(Socks5Info info)
        {
            info.Socks5Step = Socks5EnumStep.Forward;
        }
        private void ForwardUdpResponseData(Socks5Info info)
        {
            info.Data = Socks5Parser.MakeUdpResponse(socks5ClientListener.DistEndpoint, info.Data);
        }


        private bool HandleRequest(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        private bool HandleAuth(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        private bool HandleCommand(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        private bool HndleForward(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        private bool HndleForwardUdp(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }

        public void Flush()
        {
            connection = null;
            GetConnection();
        }

        private void GetConnection()
        {
            if (connection == null)
            {
                if (string.IsNullOrWhiteSpace(config.TargetName))
                {
                    connection = SelectConnection(config.TunnelType, registerStateInfo.TcpConnection, registerStateInfo.UdpConnection);
                }
                else
                {
                    var client = clientInfoCaching.GetByName(config.TargetName);
                    if (client != null)
                    {
                        connection = SelectConnection(config.TunnelType, client.TcpConnection, client.UdpConnection);
                    }
                }
            }
        }
        private IConnection SelectConnection(TunnelTypes tunnelType, IConnection tcpconnection, IConnection udpconnection)
        {
            return tunnelType switch
            {
                TunnelTypes.TCP_FIRST => tcpconnection != null ? tcpconnection : udpconnection,
                TunnelTypes.UDP_FIRST => udpconnection != null ? udpconnection : tcpconnection,
                TunnelTypes.TCP => tcpconnection,
                TunnelTypes.UDP => udpconnection,
                _ => tcpconnection,
            };
        }


    }
}
