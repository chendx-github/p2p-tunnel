using common.libs.extends;
using System;

namespace common.server.model
{
    public class ClientsInfo
    {
        public ClientsInfo() { }

        public ClientsClientInfo[] Clients { get; set; } = Array.Empty<ClientsClientInfo>();

        public byte[] ToBytes()
        {
            int length = 0, dataLength = Clients.Length;
            byte[][] dataBytes = new byte[dataLength][];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = Clients[i].ToBytes();
                length += dataBytes[i].Length;

            }

            int index = 0;
            var lengthBytes = dataLength.ToBytes();
            length += lengthBytes.Length;

            var bytes = new byte[length];

            Array.Copy(lengthBytes, 0, bytes, index, lengthBytes.Length);
            index += lengthBytes.Length;

            for (int i = 0; i < dataBytes.Length; i++)
            {
                Array.Copy(dataBytes[i], 0, bytes, index, dataBytes[i].Length);
                index += dataBytes[i].Length;
            }
            return bytes;

        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            int length = span.Slice(index, 4).ToInt32();
            index += 4;

            Clients = new ClientsClientInfo[length];
            for (int i = 0; i < length; i++)
            {
                Clients[i] = new ClientsClientInfo();
                int tempIndex = Clients[i].DeBytes(data.Slice(index));
                index += tempIndex;
            }
        }

    }

    public class ClientsClientInfo
    {
        public ulong Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection TcpConnection { get; set; } = null;
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection UdpConnection { get; set; } = null;

        public byte[] ToBytes()
        {
            var idBytes = Id.ToBytes();
            var nameBytes = Name.ToBytes();
            var macBytes = Mac.ToBytes();

            var bytes = new byte[
                8
                + 1 + nameBytes.Length
                + 1 + macBytes.Length
                ];

            int index = 0;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, bytes, index + 1, nameBytes.Length);
            index += 1 + nameBytes.Length;

            bytes[index] = (byte)macBytes.Length;
            Array.Copy(macBytes, 0, bytes, index + 1, macBytes.Length);
            index += 1 + macBytes.Length;

            return bytes;
        }

        public int DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Mac = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            return index;
        }
    }
}
