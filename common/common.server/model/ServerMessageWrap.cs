using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.ComponentModel;

namespace common.server.model
{
    public class MessageRequestWrap
    {
        public IConnection Connection { get; set; }
        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; } = 3000;


        public string Path
        {
            set
            {
                MemoryPath = value.ToLower().ToBytes();
            }
        }
        public Memory<byte> MemoryPath { get; set; } = Memory<byte>.Empty;

        public ulong RequestId { get; set; } = 0;

        /// <summary>
        /// 发送数据
        /// </summary>
        public Memory<byte> Content { get; set; } = Helper.EmptyArray;
        /// <summary>
        /// 接收数据
        /// </summary>
        public Memory<byte> Memory { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type, out int length, bool pool = false)
        {
            byte typeByte = (byte)MessageTypes.REQUEST;
            byte[] requestIdByte = RequestId.ToBytes();

            length = (type == ServerType.TCP ? 4 : 0)
                + 1
                + requestIdByte.Length
                + 1 + MemoryPath.Length
                + Content.Length;

            byte[] res = pool ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];

            int index = 0;

            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = typeByte;
            index += 1;

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            res[index] = (byte)MemoryPath.Length;
            index += 1;

            MemoryPath.CopyTo(res.AsMemory(index, MemoryPath.Length));
            index += MemoryPath.Length;

            Content.CopyTo(res.AsMemory(index, Content.Length));
            index += Content.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 1;

            RequestId = span.Slice(index).ToUInt64();
            index += 8;

            int pathLength = span[index];
            index += 1;

            MemoryPath = memory.Slice(index, pathLength);
            index += pathLength;

            Memory = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            MemoryPath = Memory<byte>.Empty;
            Content = Helper.EmptyArray;
            Memory = Helper.EmptyArray;
        }
    }
    public class MessageResponseWrap
    {
        public IConnection Connection { get; set; }
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public ulong RequestId { get; set; } = 0;
        public ReadOnlyMemory<byte> Content { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public (byte[] data, int length) ToArray(ServerType type, bool pool = false)
        {
            int length = (type == ServerType.TCP ? 4 : 0)
                + 1
                + 1
                + 8
                + Content.Length;

            byte[] res = pool ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];

            int index = 0;
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = (byte)MessageTypes.RESPONSE;
            index += 1;

            res[index] = (byte)Code;
            index += 1;

            byte[] requestIdByte = RequestId.ToBytes();
            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Content.CopyTo(res.AsMemory(index, Content.Length));
            index += Content.Length;

            return (res, length);
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 1;

            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt64();
            index += 8;

            Content = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            Content = Helper.EmptyArray;
            Content = Helper.EmptyArray;
        }
    }

    [Flags]
    public enum MessageResponeCodes : byte
    {
        /// <summary>
        /// 成功
        /// </summary>
        [Description("成功")]
        OK = 0,
        /// <summary>
        /// 网络未连接
        /// </summary>
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        /// <summary>
        /// 网络资源未找到
        /// </summary>
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        /// <summary>
        /// 网络超时
        /// </summary>
        [Description("网络超时")]
        TIMEOUT = 3,
        /// <summary>
        /// 程序错误
        /// </summary>
        [Description("程序错误")]
        ERROR = 4,
    }

    [Flags]
    public enum MessageTypes : byte
    {
        [Description("请求")]
        REQUEST = 0,
        [Description("回复")]
        RESPONSE = 1
    }

}
