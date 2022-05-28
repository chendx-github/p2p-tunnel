using common.libs;
using common.libs.extends;
using common.server;
using MessagePack;
using System;
using System.ComponentModel;
using System.Net;

namespace common.tcpforward
{
    public struct ListeningChangeInfo
    {
        public int Port { get; set; }
        public bool Listening { get; set; }
    }

    public class TcpForwardTargetInfo
    {
        public IConnection Connection { get; set; }
        public Memory<byte> Endpoint { get; set; }
    }

    [MessagePackObject]
    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }

        [Key(1)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
        [Key(2)]
        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.FORWARD;
        [Key(3)]
        public ulong RequestId { get; set; } = 0;
        [Key(4)]
        public Memory<byte> TargetEndpoint { get; set; }
        [Key(5)]
        public Memory<byte> Buffer { get; set; } = Helper.EmptyArray;

        public byte[] ToBytes()
        {
            byte[] requestIdBytes = RequestId.ToBytes();
            var bytes = new byte[
                1 + //AliveType
                1 + //ForwardType
                1 + TargetEndpoint.Length + // endpoint
                8 + //RequestId
                Buffer.Length
            ];

            int index = 0;

            bytes[index] = (byte)AliveType;
            index++;

            bytes[index] = (byte)ForwardType;
            index++;

            bytes[index] = (byte)TargetEndpoint.Length;
            index++;

            TargetEndpoint.CopyTo(bytes.AsMemory(index, TargetEndpoint.Length));
            index += TargetEndpoint.Length;

            Array.Copy(requestIdBytes, 0, bytes, index, requestIdBytes.Length);
            index += requestIdBytes.Length;

            Buffer.CopyTo(bytes.AsMemory(index, Buffer.Length));
            index += Buffer.Length;
            return bytes;
        }

        public void DeBytes(in Memory<byte> memory)
        {
            int index = 0;

            AliveType = (TcpForwardAliveTypes)memory.Span[index];
            index++;

            ForwardType = (TcpForwardTypes)memory.Span[index];
            index++;

            byte endpointLength = memory.Span[index];
            index++;

            TargetEndpoint = memory.Slice(index, endpointLength);
            index += endpointLength;

            RequestId = memory.Span.Slice(index).ToUInt64();
            index += 8;

            Buffer = memory.Slice(index);
        }
    }
    public class TcpForwardRequestInfo
    {
        public IConnection Connection { get; set; }
        public int SourcePort { get; set; } = 0;
        public TcpForwardInfo Msg { get; set; }
    }

    [Flags]
    public enum TcpForwardTypes : byte
    {
        [Description("转发")]
        FORWARD = 1,
        [Description("代理")]
        PROXY = 2
    }

    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL = 1,
        [Description("短连接")]
        WEB = 2
    }

    [Flags]
    public enum TcpForwardTunnelTypes : byte
    {
        [Description("只tcp")]
        TCP = 1 << 1,
        [Description("只udp")]
        UDP = 1 << 2,
        [Description("优先tcp")]
        TCP_FIRST = 1 << 3,
        [Description("优先udp")]
        UDP_FIRST = 1 << 4,
    }
}
