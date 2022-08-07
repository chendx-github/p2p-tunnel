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
        public int Port { get; set; } = 0;
        public int TcpPort { get; set; } = 0;
        public ulong Id { get; set; } = 0;
        public string Address { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;

        public IConnection TcpConnection { get; set; } = null;
        public IConnection UdpConnection { get; set; } = null;

        public byte[] ToBytes()
        {
            var portBytes = Port.ToBytes();
            var tcpPortBytes = TcpPort.ToBytes();
            var idBytes = Id.ToBytes();
            var addressBytes = Address.ToBytes();
            var nameBytes = Name.ToBytes();
            var macBytes = Mac.ToBytes();

            var bytes = new byte[
                2 + 2 + 8
                + 1 + addressBytes.Length
                + 1 + nameBytes.Length
                + 1 + macBytes.Length
                ];

            int index = 0;

            bytes[index] = portBytes[0];
            bytes[index + 1] = portBytes[1];
            index += 2;
            bytes[index] = tcpPortBytes[0];
            bytes[index + 1] = tcpPortBytes[1];
            index += 2;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)addressBytes.Length;
            Array.Copy(addressBytes, 0, bytes, index + 1, addressBytes.Length);
            index += 1 + addressBytes.Length;

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

            Port = span.Slice(index, 2).ToUInt16();
            index += 2;
            TcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;
            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            Address = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Mac = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            return index;
        }
    }
}
