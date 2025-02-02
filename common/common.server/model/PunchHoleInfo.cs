﻿using common.libs;
using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 打洞数据交换
    /// </summary>
    public sealed class PunchHoleRequestInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleRequestInfo() { }
        /// <summary>
        /// 数据交换分两种，一种是a让服务器把a的公网数据发给b，另一种是，a把一些数据通过服务器原样交给b
        /// </summary>
        public PunchForwardTypes PunchForwardType { get; set; }
        /// <summary>
        /// 打洞步骤，这个数据是第几步
        /// </summary>
        public byte PunchStep { get; set; }
        /// <summary>
        /// 打洞类别，tcp udp 或者其它
        /// </summary>
        public byte PunchType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte Index { get; set; }
        /// <summary>
        /// 来自谁
        /// </summary>
        public ulong FromId { get; set; }
        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; }
        /// <summary>
        /// 通道名，可能会有多个通道
        /// </summary>
        public ulong TunnelName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong RequestId { get; set; }

        /// <summary>
        /// 携带的数
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public ReadOnlyMemory<byte> Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[
                1  //PunchForwardType
                + 1 //PunchStep
                + 1   //PunchType
                + 1 //Index
                + 8  //FromId
                + 8 //ToId
                + 8 //TunnelName 
                + 8 //RequestId
                + Data.Length
                ];
            var memory = bytes.AsMemory();

            int index = 0;

            bytes[index] = (byte)PunchForwardType;
            index += 1;
            bytes[index] = PunchStep;
            index += 1;
            bytes[index] = PunchType;
            index += 1;
            bytes[index] = Index;
            index += 1;

            FromId.ToBytes(memory.Slice(index));
            index += 8;

            ToId.ToBytes(memory.Slice(index));
            index += 8;

            TunnelName.ToBytes(memory.Slice(index));
            index += 8;

            RequestId.ToBytes(memory.Slice(index));
            index += 8;

            Data.CopyTo(bytes.AsMemory(index));

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

            PunchForwardType = (PunchForwardTypes)span[index];
            index += 1;
            PunchStep = span[index];
            index += 1;
            PunchType = span[index];
            index += 1;
            Index = span[index];
            index += 1;

            FromId = span.Slice(index, 8).ToUInt64();
            index += 8;

            ToId = span.Slice(index, 8).ToUInt64();
            index += 8;

            TunnelName = span.Slice(index, 8).ToUInt64();
            index += 8;

            RequestId = span.Slice(index, 8).ToUInt64();
            index += 8;

            Data = data.Slice(index);

        }
    }

    /// <summary>
    /// 打洞消息回执
    /// </summary>
    public sealed class PunchHoleResponseInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong RequestId { get; set; } = 0;
        /// <summary>
        /// 来自谁
        /// </summary>
        public ulong FromId { get; set; } = 0;
        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[24];
            var memory = bytes.AsMemory();
            int index = 0;

            RequestId.ToBytes(memory.Slice(index));
            index += 8;
            FromId.ToBytes(memory.Slice(index));
            index += 8;
            ToId.ToBytes(memory.Slice(index));
            index += 8;
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

            RequestId = span.Slice(index, 8).ToUInt64();
            index += 8;

            FromId = span.Slice(index, 8).ToUInt64();
            index += 8;

            ToId = span.Slice(index, 8).ToUInt64();
            index += 8;
        }
    }

    /// <summary>
    /// 打洞消息类型
    /// </summary>
    [Flags]
    public enum PunchForwardTypes : byte
    {
        /// <summary>
        /// 通知
        /// </summary>
        [Description("通知A的数据给B")]
        NOTIFY = 1,
        /// <summary>
        /// 转发
        /// </summary>
        [Description("原样转发")]
        FORWARD = 0
    }

    /// <summary>
    /// 打洞消息通知数据
    /// </summary>
    public sealed class PunchHoleNotifyInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleNotifyInfo() { }

        /// <summary>
        /// 
        /// </summary>
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsDefault { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte Index { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IPAddress Ip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            int length = 0;

            for (int i = 0; i < LocalIps.Length; i++)
            {
                length += 1 + LocalIps[i].Length();
            }
            length += 1;

            length += 1; //IsDefault
            length += 1; //Index

            length += 1 + Ip.Length();

            length += 2;
            length += 2;


            var bytes = new byte[length];
            var memory = bytes.AsMemory();

            int index = 0;
            bytes[index] = (byte)LocalIps.Length;
            index += 1;
            for (int i = 0; i < LocalIps.Length; i++)
            {
                LocalIps[i].TryWriteBytes(memory.Span.Slice(index + 1), out int ll);
                bytes[index] = (byte)ll;
                index += 1 + ll;
            }

            bytes[index] = (byte)(IsDefault ? 1 : 0);
            index += 1;
            bytes[index] = Index;
            index += 1;


            Ip.TryWriteBytes(memory.Span.Slice(index + 1), out int l);
            bytes[index] = (byte)l;
            index += 1 + l;

            ((ushort)Port).ToBytes(memory.Slice(index));
            index += 2;

            ((ushort)LocalPort).ToBytes(memory.Slice(index));
            index += 2;

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

            byte ipLength = span[index];
            index += 1;
            LocalIps = new IPAddress[ipLength];
            for (byte i = 0; i < ipLength; i++)
            {
                LocalIps[i] = new IPAddress(span.Slice(index + 1, span[index]));
                index += 1 + span[index];
            }

            IsDefault = span[index] == 1;
            index += 1;

            Index = span[index];
            index += 1;
            Ip = new IPAddress(span.Slice(index + 1, span[index]));
            index += 1 + span[index];

            Port = span.Slice(index, 2).ToUInt16();
            index += 2;
            LocalPort = span.Slice(index, 2).ToUInt16();
            index += 2;
        }
    }

    /// <summary>
    /// 打洞相关消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum PunchHoleMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 400,
        /// <summary>
        /// 发送
        /// </summary>
        Request = 401,
        /// <summary>
        /// 回执
        /// </summary>
        Response = 402,
        /// <summary>
        /// 
        /// </summary>
        Max = 499,
    }
}
