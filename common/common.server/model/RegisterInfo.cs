using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    public class RegisterParamsInfo
    {
        public RegisterParamsInfo() { }

        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        public ulong Id { get; set; } = 0;

        public string GroupId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;

        public int LocalTcpPort { get; set; } = 0;
        public int LocalUdpPort { get; set; } = 0;

        public byte[] ToBytes()
        {
            int length = 0;

            byte[][] ipBytes = new byte[LocalIps.Length][];
            for (int i = 0; i < LocalIps.Length; i++)
            {
                ipBytes[i] = LocalIps[i].GetAddressBytes();
                length += 1 + ipBytes[i].Length;
            }
            length += 1;

            var idBytes = Id.ToBytes();
            length += 8;
            var groupidBytes = GroupId.ToBytes();
            length += 1 + groupidBytes.Length;
            var nameBytes = Name.ToBytes();
            length += 1 + nameBytes.Length;
            var macBytes = Mac.ToBytes();
            length += 1 + macBytes.Length;
            var keyBytes = Key.ToBytes();
            length += 1 + keyBytes.Length;
            var localtcpPort = LocalTcpPort.ToBytes();
            length += 2;
            var localudpPort = LocalUdpPort.ToBytes();
            length += 2;

            var bytes = new byte[length];
            int index = 0;
            bytes[index] = (byte)ipBytes.Length;
            index += 1;
            for (int i = 0; i < ipBytes.Length; i++)
            {
                bytes[index] = (byte)ipBytes[i].Length;
                Array.Copy(ipBytes[i], 0, bytes, index + 1, ipBytes[i].Length);
                index += 1 + ipBytes[i].Length;
            }

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)groupidBytes.Length;
            index += 1;
            Array.Copy(groupidBytes, 0, bytes, index, groupidBytes.Length);
            index += groupidBytes.Length;

            bytes[index] = (byte)nameBytes.Length;
            index += 1;
            Array.Copy(nameBytes, 0, bytes, index, nameBytes.Length);
            index += nameBytes.Length;

            bytes[index] = (byte)macBytes.Length;
            index += 1;
            Array.Copy(macBytes, 0, bytes, index, macBytes.Length);
            index += macBytes.Length;

            bytes[index] = (byte)keyBytes.Length;
            index += 1;
            Array.Copy(keyBytes, 0, bytes, index, keyBytes.Length);
            index += keyBytes.Length;

            bytes[index] = localtcpPort[0];
            bytes[index + 1] = localtcpPort[1];
            index += 2;

            bytes[index] = localudpPort[0];
            bytes[index + 1] = localudpPort[1];
            index += 2;

            return bytes;

        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            byte ipLength = span[index];
            index += 1;
            LocalIps = new IPAddress[ipLength];
            for (byte i = 0; i < ipLength; i++)
            {
                LocalIps[i] = new IPAddress(span.Slice(index + 1, span[index]));
                index += 1 + span[index];
            }

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            GroupId = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Mac = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Key = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            LocalTcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;
            LocalUdpPort = span.Slice(index, 2).ToUInt16();
            index += 2;
        }
    }

    public class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        public RegisterResultInfoCodes Code { get; set; } = RegisterResultInfoCodes.OK;
        public bool Relay { get; set; } = false;

        public int UdpPort { get; set; } = 0;
        public int TcpPort { get; set; } = 0;

        public int TimeoutDelay { get; set; } = 0;

        public ulong Id { get; set; } = 0;
        public IPAddress Ip { get; set; } = IPAddress.Any;
        public string GroupId { get; set; } = string.Empty;

        public byte[] ToBytes()
        {
            var udpPortBytes = UdpPort.ToBytes();
            var tcpPortBytes = TcpPort.ToBytes();
            var timeoutBytes = TimeoutDelay.ToBytes();
            var idBytes = Id.ToBytes();
            var ipBytes = Ip.GetAddressBytes();
            var groupIdBytes = GroupId.ToBytes();

            var bytes = new byte[
                1 + 1
                + 2 + 2
                + timeoutBytes.Length
                + 8
                + 1 + ipBytes.Length
                + groupIdBytes.Length];

            int index = 0;
            bytes[index] = (byte)Code;
            index += 1;

            bytes[index] = (byte)(Relay ? 1 : 0);
            index += 1;

            bytes[index] = udpPortBytes[0];
            bytes[index + 1] = udpPortBytes[1];
            index += 2;

            bytes[index] = tcpPortBytes[0];
            bytes[index + 1] = tcpPortBytes[1];
            index += 2;

            Array.Copy(timeoutBytes, 0, bytes, index, timeoutBytes.Length);
            index += 4;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)ipBytes.Length;
            Array.Copy(ipBytes, 0, bytes, index + 1, ipBytes.Length);
            index += 1 + ipBytes.Length;

            Array.Copy(groupIdBytes, 0, bytes, index, groupIdBytes.Length);
            index += groupIdBytes.Length;

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;

            int index = 0;
            Code = (RegisterResultInfoCodes)span[index];
            index += 1;

            Relay = span[index] == 0 ? false : true;
            index += 1;

            UdpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TimeoutDelay = span.Slice(index, 4).ToInt32();
            index += 4;

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;


            Ip = new IPAddress(span.Slice(index + 1, span[index]));
            index += 1 + span[index];

            GroupId = span.Slice(index).GetString();
        }

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
}
