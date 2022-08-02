using common.libs.messageFormatters;
using MessagePack;
using System;
using System.IO;
using System.IO.Compression;

namespace common.libs.extends
{
    public static class ByteArrayExtends
    {
        static MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        static MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(MessageFormatterResolver.Instance);
        public static byte[] ToBytes<T>(this T obj)
        {
            if (obj is byte[] bytes)
            {
                return bytes;
            }
            return MessagePackSerializer.Serialize(obj, options);
        }
        public static Memory<byte> ToMemory<T>(this T obj)
        {
            if (obj is byte[] bytes)
            {
                return bytes;
            }
            if (obj is Memory<byte> memorys)
            {
                return memorys;
            }
            return MessagePackSerializer.Serialize(obj, options);
        }

        public static byte[] ToBytesWithCompression<T>(this T obj)
        {
            return MessagePackSerializer.Serialize(obj, lz4Options);
        }
        public static T DeBytes<T>(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, options);
        }
        public static T DeBytes<T>(this Memory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, options);
        }
        public static T DeBytes<T>(this ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, options);
        }

        public static T DeBytesWithCompression<T>(this byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }
        public static T DeBytesWithCompression<T>(this Memory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }
        public static T DeBytesWithCompression<T>(this ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, lz4Options);
        }

        public static byte[] GZip(this byte[] bytes)
        {
            using MemoryStream compressStream = new MemoryStream();
            using var zipStream = new GZipStream(compressStream, CompressionMode.Compress);
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();//不先关闭会有 解压结果为0的bug
            return compressStream.ToArray();
        }
        public static byte[] UnGZip(this byte[] bytes)
        {
            using var compressStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}
