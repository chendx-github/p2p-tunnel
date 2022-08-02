using MessagePack;
using MessagePack.Formatters;
using System;
using System.Net;

namespace common.libs.messageFormatters
{
    public class MessagePackIPAddressFormatter : IMessagePackFormatter<IPAddress>
    {
        public static readonly MessagePackIPAddressFormatter Instance = new MessagePackIPAddressFormatter();

        public IPAddress Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return IPAddress.Parse(reader.ReadString());
        }

        public void Serialize(ref MessagePackWriter writer, IPAddress value, MessagePackSerializerOptions options)
        {
            writer.Write(value.ToString());
        }
    }
}
