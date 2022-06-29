using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.Net;

namespace common.libs.messageFormatters
{
    public class MessageFormatterResolver : IFormatterResolver
    {
        public static readonly MessageFormatterResolver Instance = new MessageFormatterResolver();
        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
                StandardResolver.Instance
        };

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(IPAddress))
            {
                return (IMessagePackFormatter<T>)MessagePackIPAddressFormatter.Instance;
            }
            foreach (var resolver in Resolvers)
            {
                var f = resolver.GetFormatter<T>();
                if (f != null)
                {
                    return f;
                }
            }
            return null;
        }
    }
}
