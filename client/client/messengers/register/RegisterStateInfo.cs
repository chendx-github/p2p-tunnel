using common.libs;
using MessagePack;
using common.server;
using System;
using System.Net;

namespace client.messengers.register
{
    /// <summary>
    /// 本地注册状态
    /// </summary>
    public class RegisterStateInfo
    {
        /// <summary>
        /// TCP连接对象
        /// </summary>
        public IConnection TcpConnection { get; set; }
        public bool TcpOnline => TcpConnection != null && ConnectId > 0;
        /// <summary>
        /// UDP连接对象
        /// </summary>
        public IConnection UdpConnection { get; set; }
        public bool UdpOnline => UdpConnection != null && ConnectId > 0;
        /// <summary>
        /// 远程信息
        /// </summary>
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
        /// <summary>
        /// 本地信息
        /// </summary>
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();

        public SimpleSubPushHandler<bool> OnRegisterStateChange { get; } = new SimpleSubPushHandler<bool>();

        private ulong connectid = 0;
        public ulong ConnectId
        {
            get
            {
                return connectid;
            }
            set
            {
                connectid = value;
                RemoteInfo.ConnectId = connectid;

                if (connectid == 0)
                {
                    if (TcpConnection != null)
                    {
                        TcpConnection.Disponse();
                    }
                    TcpConnection = null;

                    if (UdpConnection != null)
                    {
                        UdpConnection.Disponse();
                    }
                    UdpConnection = null;
                }
                else
                {
                    UdpConnection.ConnectId = connectid;
                    TcpConnection.ConnectId = connectid;
                }
            }
        }

        public void Offline()
        {
            LocalInfo.IsConnecting = false;
            LocalInfo.UdpConnected = false;
            LocalInfo.TcpConnected = false;

            RemoteInfo.Ip = IPAddress.Any;
            RemoteInfo.UdpPort = 0;
            RemoteInfo.TcpPort = 0;

            ConnectId = 0;

            OnRegisterStateChange.Push(false);

            if (TcpConnection != null)
            {
                TcpConnection.Disponse();
                TcpConnection = null;
            }
            if (UdpConnection != null)
            {
                UdpConnection.Disponse();
                UdpConnection = null;
            }
        }
        public void Online(ulong id, IPAddress ip, int udpPort, int tcpPort)
        {
            LocalInfo.IsConnecting = false;
            LocalInfo.UdpConnected = true;
            LocalInfo.TcpConnected = true;

            RemoteInfo.Ip = ip;
            RemoteInfo.UdpPort = udpPort;
            RemoteInfo.TcpPort = tcpPort;

            ConnectId = id;

            OnRegisterStateChange.Push(true);
        }
    }

    /// <summary>
    /// 远程信息
    /// </summary>
    public class RemoteInfo
    {
        /// <summary>
        /// 客户端在远程的ip
        /// </summary>
        public IPAddress Ip { get; set; } = IPAddress.Any;
        /// <summary>
        /// 客户端在远程的TCP端口
        /// </summary>
        public int UdpPort { get; set; } = 0;

        public int TcpPort { get; set; } = 0;
        /// <summary>
        /// 客户端连接ID
        /// </summary>
        public ulong ConnectId { get; set; } = 0;

        public bool Relay { get; set; } = false;
    }

    /// <summary>
    /// 本地信息
    /// </summary>
    public class LocalInfo
    {
        /// <summary>
        /// 外网距离
        /// </summary>
        public int RouteLevel { get; set; } = 0;
        /// <summary>
        /// 本地mac地址
        /// </summary>
        public string Mac { get; set; } = string.Empty;
        /// <summary>
        /// 本地UDP端口
        /// </summary>
        public int UdpPort { get; set; } = 0;
        /// <summary>
        /// 本地TCP端口
        /// </summary>
        public int TcpPort { get; set; } = 0;

        public IPAddress LocalIp { get; set; } = IPAddress.Any;

        /// <summary>
        /// 是否正在连接服务器
        /// </summary>
        public bool IsConnecting { get; set; } = false;
        /// <summary>
        /// UDP是否已连接服务器
        /// </summary>
        public bool UdpConnected { get; set; } = false;

        [System.Text.Json.Serialization.JsonIgnore]
        public SimpleSubPushHandler<bool> TcpConnectedSub { get; } = new SimpleSubPushHandler<bool>();

        private bool tcpConnected = false;
        /// <summary>
        /// TCP是否已连接服务器
        /// </summary>
        public bool TcpConnected
        {
            get
            {
                return tcpConnected;
            }
            set
            {
                tcpConnected = value;
                TcpConnectedSub.Push(value);
            }
        }
    }
}
