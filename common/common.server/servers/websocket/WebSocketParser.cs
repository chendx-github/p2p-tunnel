using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace common.server.servers.websocket
{
    public static class WebSocketParser
    {
        private readonly static SHA1 sha1 = SHA1.Create();
        private readonly static Memory<byte> MagicCode = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        public static byte[] BuildConnectData(WebsocketHeaderInfo header)
        {
            int keyLength = header.SecWebSocketKey.Length + MagicCode.Length;
            byte[] keyBytes = ArrayPool<byte>.Shared.Rent(keyLength);

            header.SecWebSocketKey.CopyTo(keyBytes);
            MagicCode.CopyTo(keyBytes.AsMemory(header.SecWebSocketKey.Length));

            string acceptStr = Convert.ToBase64String(sha1.ComputeHash(keyBytes, 0, keyLength));
            ArrayPool<byte>.Shared.Return(keyBytes);

            StringBuilder sb = new StringBuilder(10);
            sb.Append($"HTTP/1.1 {(int)header.StatusCode} {AddSpace(header.StatusCode)}\r\n");
            sb.Append($"Sec-WebSocket-Accept: {acceptStr}\r\n");
            if (header.Connection.Length > 0)
            {
                sb.Append($"Connection: {header.Connection.GetString()}\r\n");
            }
            if (header.Upgrade.Length > 0)
            {
                sb.Append($"Upgrade: {header.Upgrade.GetString()}\r\n");
            }
            if (header.SecWebSocketVersion.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Version: {header.SecWebSocketVersion.GetString()}\r\n");
            }
            if (header.SecWebSocketProtocol.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Protocol: {header.SecWebSocketProtocol.GetString()}\r\n");
            }
            if (header.SecWebSocketExtensions.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Extensions: {header.SecWebSocketExtensions.GetString()}\r\n");
            }
            sb.Append("\r\n");

            return sb.ToString().ToBytes();
        }

        public static byte[] BuildPongData()
        {
            return new byte[]
            {
                0x80 & 0xa, //fin + pong
                0, //没有 mask 和 payload length
            };
        }
        public static byte[] BuildFrameData(Memory<byte> data, WebSocketFrameInfo.EnumOpcode opcode)
        {
            int length = 1 + 1 + data.Length, index = 0;
            byte payloadLength;
            byte[] payloadLengthBytes = Helper.EmptyArray;

            int dataLength = data.Length;

            if (dataLength > ushort.MaxValue)
            {
                length += 8;
                payloadLength = 127;
                payloadLengthBytes = BinaryPrimitives.ReverseEndianness(((ulong)dataLength)).ToBytes();
            }
            else if (dataLength > 125)
            {
                length += 2;
                payloadLength = 126;
                payloadLengthBytes = BinaryPrimitives.ReverseEndianness(((ushort)dataLength)).ToBytes();
            }
            else
            {
                payloadLength = (byte)dataLength;
            }

            byte[] bytes = new byte[length];

            bytes[0] = (byte)(0x80 | (byte)opcode);
            bytes[1] = payloadLength;
            index += 2;


            if (payloadLengthBytes.Length > 0)
            {
                Array.Copy(payloadLengthBytes, 0, bytes, index, payloadLengthBytes.Length);
                index += payloadLengthBytes.Length;
            }

            if (data.Length > 0)
            {
                data.CopyTo(bytes.AsMemory(index));
            }

            return bytes;
        }

        private static string AddSpace(HttpStatusCode statusCode)
        {
            ReadOnlySpan<char> span = statusCode.ToString().AsSpan();

            int totalLength = span.Length * 2;

            char[] result = ArrayPool<char>.Shared.Rent(totalLength);
            Span<char> resultSpan = result.AsSpan(0, totalLength);
            span.CopyTo(resultSpan);

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (i > 0 && span[i] >= 65 && span[i] <= 90)
                {
                    resultSpan.Slice(i + length, totalLength - (length + i) - 1).CopyTo(resultSpan.Slice(i + length + 1));
                    resultSpan[i + length] = (char)32;
                    length++;
                }
            }

            string resultStr = resultSpan.Slice(0, span.Length + length).ToString();
            ArrayPool<char>.Shared.Return(result);

            return resultStr;
        }
    }

    public class WebSocketFrameInfo
    {
        /// <summary>
        /// 是否是结束帧
        /// </summary>
        public EnumFin Fin { get; set; }
        /// <summary>
        /// 保留字段
        /// </summary>
        public byte Rsv { get; set; }
        /// <summary>
        /// 操作码 0附加数据，1文本，2二进制，3-7为非控制保留，8关闭，9ping，a pong，b-f 为控制保留
        /// </summary>
        public EnumOpcode Opcode { get; set; }
        /// <summary>
        /// 总长度
        /// </summary>
        public int TotalLength { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public Memory<byte> PayloadData { get; set; }

        public static bool TryParse(Memory<byte> data, out WebSocketFrameInfo frameInfo)
        {
            frameInfo = null;

            //小于6字节不可解析
            if (data.Length < 6)
            {
                return false;
            }

            Span<byte> span = data.Span;
            int index = 0;

            //第一字节
            //1位 是否是结束帧
            EnumFin fin = (EnumFin)(byte)((span[0] & 0x80) >> 7);
            //2 3 4 保留
            byte rsv = (byte)(span[0] & 0x7);
            //5 6 7 8 操作码
            EnumOpcode opcode = (EnumOpcode)(byte)(span[0] & 0x0f);
            //第2字节
            //1位 是否mask
            byte mask = (byte)((span[1] & 0x80) >> 7);
            int payloadLength = (span[1] & 0x7f);
            index += 2;

            if (payloadLength == 126)
            {
                ushort pl = span.Slice(2, 2).ToUInt16();
                payloadLength = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(pl) : pl;
                index += 2;
            }
            else if (payloadLength == 127)
            {
                ulong pl = span.Slice(2, 8).ToUInt64();
                payloadLength = (int)(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(pl) : pl);
                index += 8;
            }

            //数据长+头长 大于 整个数据长，则不是一个完整的包
            if (data.Length < payloadLength + index + 4)
            {
                return false;
            }

            //mask码 用来解码数据
            Span<byte> maskKey = span.Slice(index, 4);
            index += 4;

            //数据
            Memory<byte> payloadData = data.Slice(index, payloadLength);
            Span<byte> payloadDataSpan = span.Slice(index, payloadLength);
            //解码
            for (int i = 0; i < payloadDataSpan.Length; i++)
            {
                payloadDataSpan[i] = (byte)(payloadDataSpan[i] ^ maskKey[3 & i]);
            }

            frameInfo = new WebSocketFrameInfo
            {
                Fin = fin,
                Rsv = rsv,
                Opcode = opcode,
                PayloadData = payloadData,
                TotalLength = index + payloadLength
            };
            return true;
        }

        public enum EnumFin : byte
        {
            UnFin = 0x0,
            Fin = 0x1,
        }

        public enum EnumOpcode : byte
        {
            Data = 0x0,
            Text = 0x1,
            Binary = 0x2,
            UnControll3 = 0x3,
            UnControll4 = 0x4,
            UnControll5 = 0x5,
            UnControll6 = 0x6,
            UnControll7 = 0x7,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xa,
            Controll11 = 0xb,
            Controll12 = 0xc,
            Controll13 = 0xd,
            Controll14 = 0xe,
            Controll15 = 0xf,
        }
    }

    public class WebsocketHeaderInfo
    {
        static byte[][] bytes = new byte[][] {
            Encoding.ASCII.GetBytes("Connection: "),
            Encoding.ASCII.GetBytes("Upgrade: "),
            Encoding.ASCII.GetBytes("Origin: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Version: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Key: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Extensions: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Protocol: ")
        };

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.SwitchingProtocols;
        /// <summary>
        /// 如果 仅1个字符，那就是 /
        /// </summary>
        public Memory<byte> Path { get; private set; }
        public Memory<byte> Connection { get; private set; }
        public Memory<byte> Upgrade { get; private set; }
        public Memory<byte> Origin { get; private set; }
        public Memory<byte> SecWebSocketVersion { get; set; }
        public Memory<byte> SecWebSocketKey { get; private set; }
        public Memory<byte> SecWebSocketExtensions { get; set; }
        public Memory<byte> SecWebSocketProtocol { get; set; }

        public static WebsocketHeaderInfo Parse(Memory<byte> header)
        {
            Span<byte> span = header.Span;
            int flag = 0xff;
            int bit = 0x01;

            ulong[] res = new ulong[bytes.Length];

            for (int i = 0, len = span.Length; i < len; i++)
            {
                if (span[i] == 13 && span[i + 1] == 10 && span[i + 2] == 13 && span[i + 3] == 10)
                {
                    break;
                }
                if (span[i] == 13 && span[i + 1] == 10)
                {
                    int startIndex = i + 2;
                    for (int k = 0; k < bytes.Length; k++)
                    {
                        if ((flag >> k & 1) == 1 && span[startIndex] == bytes[k][0])
                        {
                            if (span.Slice(startIndex, bytes[k].Length).SequenceEqual(bytes[k]))
                            {
                                int index = span.Slice(startIndex).IndexOf((byte)13);
                                flag &= ~(bit << k);

                                res[k] = ((ulong)(startIndex + bytes[k].Length) << 32) | (ulong)(index - bytes[k].Length);

                                i += index + 1;
                                break;
                            }
                        }
                    }
                }
            }

            int pathIndex = span.IndexOf((byte)32) + 1;
            int pathIndex1 = span.Slice(pathIndex + 1).IndexOf((byte)32);

            return new WebsocketHeaderInfo
            {
                Path = header.Slice(pathIndex, pathIndex1),
                Connection = header.Slice((int)(res[0] >> 32), (int)(res[0] & 0xffffffff)),
                Upgrade = header.Slice((int)(res[1] >> 32), (int)(res[1] & 0xffffffff)),
                Origin = header.Slice((int)(res[2] >> 32), (int)(res[2] & 0xffffffff)),
                SecWebSocketVersion = header.Slice((int)(res[3] >> 32), (int)(res[3] & 0xffffffff)),
                SecWebSocketKey = header.Slice((int)(res[4] >> 32), (int)(res[4] & 0xffffffff)),
                SecWebSocketExtensions = header.Slice((int)(res[5] >> 32), (int)(res[5] & 0xffffffff)),
                SecWebSocketProtocol = header.Slice((int)(res[6] >> 32), (int)(res[6] & 0xffffffff)),
            };
        }
    }
}
