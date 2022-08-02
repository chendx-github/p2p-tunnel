using common.libs.extends;
using MessagePack;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    [MessagePackObject]
    public class RegisterParamsInfo
    {
        public RegisterParamsInfo() { }

        [Key(1)]
        public ulong Id { get; set; } = 0;

        [Key(2)]
        public string GroupId { get; set; } = string.Empty;

        [Key(3)]
        public string Name { get; set; } = string.Empty;

        [Key(4)]
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();

        [Key(5)]
        public string Mac { get; set; } = string.Empty;

        [Key(6)]
        public int LocalTcpPort { get; set; } = 0;

        [Key(7)]
        public int LocalUdpPort { get; set; } = 0;

        [Key(8)]
        public string Key { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        [Key(1)]
        public RegisterResultInfoCodes Code { get; set; } = RegisterResultInfoCodes.OK;

        [Key(2)]
        public ulong Id { get; set; } = 0;

        [Key(3)]
        public IPAddress Ip { get; set; } = IPAddress.Any;

        [Key(4)]
        public int Port { get; set; } = 0;

        [Key(5)]
        public int TcpPort { get; set; } = 0;

        [Key(6)]
        public string GroupId { get; set; } = string.Empty;


        [Key(7)]
        public bool Relay { get; set; } = false;


        [Flags]
        public enum RegisterResultInfoCodes : byte
        {
            [Description("成功")]
            OK = 1,
            [Description("存在同名")]
            SAME_NAMES = 2,
            [Description("验证未通过")]
            VERIFY = 4,
            [Description("key验证未通过")]
            KEY_VERIFY = 8,
            [Description("出错")]
            UNKNOW = 16
        }
    }

    public class TunnelRegisterParamsInfo
    {
        public TunnelRegisterParamsInfo() { }

        public string TunnelName { get; set; } = string.Empty;

        public int LocalPort { get; set; } = 0;

        public int Port { get; set; } = 0;

        public byte[] ToBytes()
        {
            byte[] lportBytes = LocalPort.ToBytes();
            byte[] portBytes = Port.ToBytes();
            byte[] tnameBytes = TunnelName.ToBytes();

            var bytes = new byte[lportBytes.Length + portBytes.Length + tnameBytes.Length];

            Array.Copy(lportBytes, 0, bytes, 0, lportBytes.Length);
            Array.Copy(portBytes, 0, bytes, 4, portBytes.Length);
            Array.Copy(tnameBytes, 0, bytes, 8, tnameBytes.Length);

            return bytes;
        }
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            LocalPort = span.Slice(0, 4).ToInt32();
            Port = span.Slice(4, 4).ToInt32();
            TunnelName = span.Slice(8).GetString();
        }
    }

    [MessagePackObject]
    public class TunnelRegisterInfo
    {
        public TunnelRegisterInfo() { }

        [Key(1)]
        public TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.OK;
        [Key(2)]
        public int Port { get; set; } = 0;
    }

    [MessagePackObject]
    public class TunnelRegisterResultInfo
    {
        public TunnelRegisterResultInfo() { }

        [Key(1)]
        public TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfoCodes.OK;

        [Flags]
        public enum TunnelRegisterResultInfoCodes : byte
        {
            [Description("成功")]
            OK = 1,
            [Description("存在同名")]
            SAME_NAMES = 2,
            [Description("验证未通过")]
            VERIFY = 4,
            [Description("未连接服务器")]
            UN_CONNECT = 8,
            [Description("出错")]
            UNKNOW = 16
        }
    }
}
