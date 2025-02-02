﻿using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UdpForwardRegisterParamsInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpForwardRegisterParamsInfo() { }

        /// <summary>
        /// 
        /// </summary>
        public ushort SourcePort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TargetName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TargetIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort TargetPort { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var tipBytes = TargetIp.GetUTF16Bytes();
            var tnameBytes = TargetName.GetUTF16Bytes();

            byte[] bytes = new byte[
                2 + 2
                + 1 + 1 + tipBytes.Length
                + 1 + 1 + tnameBytes.Length
            ];
            var memory = bytes.AsMemory();
            var span = bytes.AsSpan();

            int index = 0;

            SourcePort.ToBytes(memory.Slice(index));
            index += 2;

            TargetPort.ToBytes(memory.Slice(index));
            index += 2;


            bytes[index] = (byte)tipBytes.Length;
            index += 1;
            bytes[index] = (byte)TargetIp.Length;
            index += 1;
            tipBytes.CopyTo(span.Slice(index));
            index += tipBytes.Length;

            bytes[index] = (byte)tnameBytes.Length;
            index += 1;
            bytes[index] = (byte)TargetName.Length;
            index += 1;
            tnameBytes.CopyTo(span.Slice(index));
            index += tnameBytes.Length;

            return bytes;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            SourcePort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TargetIp = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];

            TargetName = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class UdpForwardRegisterResult
    {
        /// <summary>
        /// 
        /// </summary>
        public UdpForwardRegisterResultCodes Code { get; set; } = UdpForwardRegisterResultCodes.OK;
        /// <summary>
        /// 
        /// </summary>
        public ulong ID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var msgBytes = Msg.GetUTF16Bytes();
            var bytes = new byte[
                1
                + 8
                + 1 + 1 + msgBytes.Length
            ];
            var memory = bytes.AsMemory();
            var span = bytes.AsSpan();

            int index = 0;

            bytes[index] = (byte)Code;
            index += 1;

            ID.ToBytes(memory.Slice(index));
            index += 8;

            bytes[index] = (byte)msgBytes.Length;
            index += 1;
            bytes[index] = (byte)Msg.Length;
            index += 1;
            msgBytes.CopyTo(span.Slice(index));

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Code = (UdpForwardRegisterResultCodes)span[index];
            index += 1;

            ID = span.Slice(index, 8).ToUInt64();
            index += 8;

            Msg = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum UdpForwardRegisterResultCodes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("成功")]
        OK = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("插件未开启")]
        DISABLED = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("已存在")]
        EXISTS = 4,
        /// <summary>
        /// 
        /// </summary>
        [Description("端口超出范围")]
        OUT_RANGE = 8,
        /// <summary>
        /// 
        /// </summary>
        [Description("未知")]
        UNKNOW = 16,
    }
}
