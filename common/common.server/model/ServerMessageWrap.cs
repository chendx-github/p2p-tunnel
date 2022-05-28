using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Text;

namespace common.server.model
{
    public class MessageRequestWrap
    {
        public Memory<byte> Path { get; set; } = Memory<byte>.Empty;

        public ulong RequestId { get; set; } = 0;
        /// <summary>
        /// 发送数据
        /// </summary>
        public byte[] Content { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 接收数据
        /// </summary>
        public Memory<byte> Memory { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public (byte[] data, int length) ToArray(ServerType type, bool pool = false)
        {
            byte typeByte = (byte)MessageTypes.REQUEST;
            byte[] requestIdByte = RequestId.ToBytes();
            byte[] pathLengthByte = Path.Length.ToBytes();

            int length = (type == ServerType.TCP ? 4 : 0)
                + 1
                + requestIdByte.Length
                + pathLengthByte.Length + Path.Length
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

            Array.Copy(pathLengthByte, 0, res, index, pathLengthByte.Length);
            index += pathLengthByte.Length;

            Path.Span.CopyTo(res.AsSpan(index, Path.Length));
            index += Path.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;

            return (res, length);
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            int index = 1;

            RequestId = memory.Span.Slice(index).ToUInt64();
            index += 8;

            int pathLength = memory.Span.Slice(index).ToInt32();
            index += 4;

            Path = memory.Slice(index, pathLength);
            index += pathLength;

            Memory = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            Path = Memory<byte>.Empty;
            Content = Helper.EmptyArray;
            Memory = Helper.EmptyArray;
        }
    }
    public class MessageResponseWrap
    {
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
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
            int index = 1;

            Code = (MessageResponeCodes)memory.Span[index];
            index += 1;

            RequestId = memory.Span.Slice(index).ToUInt64();
            index += 8;

            Memory = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            Content = Helper.EmptyArray;
            Memory = Helper.EmptyArray;
        }
    }

    [Flags]
    public enum MessageResponeCodes : byte
    {
        [Description("成功")]
        OK = 0,
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        [Description("网络超时")]
        TIMEOUT = 3,
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

    public class MessageRequestParamsInfo<T>
    {
        public IConnection Connection { get; set; }

        public string Path
        {
            set
            {
                MemoryPath = value.ToLower().GetBytes().AsMemory();
            }
        }
        public Memory<byte> MemoryPath { get; set; }

        public T Data { get; set; } = default;
        public ulong RequestId { get; set; } = 0;
        public int Timeout { get; set; } = 15000;
    }

    public class MessageResponseParamsInfo
    {
        public IConnection Connection { get; set; }
        public Memory<byte> Data { get; set; } = Helper.EmptyArray;
        public ulong RequestId { get; set; } = 0;
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
    }
}
