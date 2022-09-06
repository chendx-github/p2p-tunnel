﻿using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.ComponentModel;

namespace common.tcpforward
{
    public struct ListeningChangeInfo
    {
        public int Port { get; set; }
        public bool State { get; set; }
    }

    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }
        public int SourcePort { get; set; }

        public bool IsForward { get; set; }
        public TcpForwardAliveTypes AliveType { get; set; }
        public TcpForwardTypes ForwardType { get; set; }
        public ulong RequestId { get; set; }
        public Memory<byte> TargetEndpoint { get; set; }
        public Memory<byte> Buffer { get; set; }
        /// <summary>
        /// 线路socket链接对象
        /// </summary>
        public IConnection Connection { get; set; }

        public byte[] ToBytes()
        {
            byte[] requestIdBytes = RequestId.ToBytes();
            int length = 1 + requestIdBytes.Length + Buffer.Length;
            byte isForward = 1;
            if (IsForward == false)
            {
                length += TargetEndpoint.Length;
                isForward = 0;
            }

            byte[] bytes = new byte[length];
            int index = 1;
            /*
                0b111 1111  
                AliveType               1bit
                ForwardType             1bit
                isForward               1bit
                TargetEndpoint.Length   4bit
             */
            bytes[0] = (byte)(((byte)AliveType - 1) << 7 | ((byte)ForwardType - 1) << 6 | isForward << 5 | (byte)TargetEndpoint.Length);

            //转发阶段不需要这些
            if (IsForward == false)
            {
                TargetEndpoint.CopyTo(bytes.AsMemory(index, TargetEndpoint.Length));
                index += TargetEndpoint.Length;
            }

            Array.Copy(requestIdBytes, 0, bytes, index, requestIdBytes.Length);
            index += requestIdBytes.Length;

            Buffer.CopyTo(bytes.AsMemory(index, Buffer.Length));
            index += Buffer.Length;
            return bytes;
        }

        public void DeBytes(in Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 1;

            byte isForward = (byte)((span[0] >> 5) & 0b1);
            byte epLength = (byte)(span[0] & 0b11111);

            //转发阶段不需要这些
            if (isForward == 0)
            {
                AliveType = (TcpForwardAliveTypes)(byte)(((span[0] >> 7) & 0b1) + 1);
                ForwardType = (TcpForwardTypes)(byte)(((span[0] >> 6) & 0b1) + 1);

                TargetEndpoint = memory.Slice(index, epLength);
                index += epLength;
            }


            RequestId = span.Slice(index).ToUInt64();
            index += 8;

            Buffer = memory.Slice(index);
        }
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
