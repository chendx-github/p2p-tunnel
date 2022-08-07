using common.libs.extends;
using System;

namespace server.service.webrtc.models
{
    public class WebRTCConnectionInfo
    {
        public WebRTCConnectionInfo() { }

        public ulong FromId { get; set; } = 0;

        public ulong ToId { get; set; } = 0;

        public string Data { get; set; } = string.Empty;

        public byte[] ToBytes()
        {
            byte[] fidBytes = FromId.ToBytes();
            byte[] tidBytes = FromId.ToBytes();
            byte[] dataBytes = Data.ToBytes();

            var bytes = new byte[fidBytes.Length + tidBytes.Length + dataBytes.Length];

            Array.Copy(fidBytes, 0, bytes, 0, fidBytes.Length);
            Array.Copy(tidBytes, 0, bytes, 8, tidBytes.Length);
            Array.Copy(dataBytes, 0, bytes, 16, dataBytes.Length);

            return bytes;
        }

        public void DeBytes(Memory<byte> data)
        {
            var span = data.Span;

            FromId = span.Slice(0, 8).ToUInt64();
            ToId = span.Slice(8, 8).ToUInt64();
            Data = span.Slice(16).GetString();
        }
    }
}
