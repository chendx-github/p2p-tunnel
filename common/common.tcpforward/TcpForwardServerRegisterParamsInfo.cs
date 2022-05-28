using MessagePack;
using System;
using System.ComponentModel;
using System.Net;

namespace common.tcpforward
{
    /// <summary>
    /// Tcp转发
    /// </summary>
    [MessagePackObject]
    public class TcpForwardRegisterParamsInfo
    {
        public TcpForwardRegisterParamsInfo() { }

        [Key(1)]
        public string SourceIp { get; set; } = IPAddress.Any.ToString();
        [Key(2)]
        public int SourcePort { get; set; } = 8080;
        [Key(3)]
        public string TargetName { get; set; } = string.Empty;
        [Key(4)]
        public string TargetIp { get; set; } = IPAddress.Loopback.ToString();
        [Key(5)]
        public int TargetPort { get; set; } = 8080;
        [Key(6)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
        [Key(7)]
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;
    }

    [MessagePackObject]
    public class TcpForwardUnRegisterParamsInfo
    {
        public TcpForwardUnRegisterParamsInfo() { }

        [Key(1)]
        public string SourceIp { get; set; } = IPAddress.Any.ToString();
        [Key(2)]
        public int SourcePort { get; set; } = 8080;
        [Key(3)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
    }

    [MessagePackObject]
    public class TcpForwardRegisterResult
    {
        [Key(1)]
        public TcpForwardRegisterResultCodes Code { get; set; } = TcpForwardRegisterResultCodes.OK;
        [Key(2)]
        public string Msg { get; set; } = String.Empty;
        [Key(3)]
        public ulong ID { get; set; } = 0;
    }

    [Flags]
    public enum TcpForwardRegisterResultCodes : byte
    {
        [Description("成功")]
        OK = 1,
        [Description("插件未开启")]
        DISABLED = 2,
        [Description("已存在")]
        EXISTS = 4,
        [Description("端口超出范围")]
        OUT_RANGE = 8,
        [Description("未知")]
        UNKNOW = 16,
    }
}
