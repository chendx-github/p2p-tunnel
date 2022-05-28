using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Text;

namespace server.service.messengers
{
    public class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        private readonly IClientRegisterCaching clientRegisterCache;
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, IClientRegisterCaching clientRegisterCache)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.clientRegisterCache = clientRegisterCache;
        }

        public string Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey;
        }

        public bool Set(IConnection connection)
        {
            var memory = asymmetricCrypto.Decode(connection.ReceiveRequestWrap.Memory);
            CryptoSetParamsInfo model = memory.DeBytes<CryptoSetParamsInfo>();

            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(model.Password);
            connection.EncodeEnable(encoder);
            return true;
        }
        public bool Test(IConnection connection)
        {
            CryptoTestParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<CryptoTestParamsInfo>();

            Console.WriteLine($"encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(model.Content).Span)}");

            return true;
        }
        public bool Clear(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                client.UdpConnection.EncodeDisable();
                client.TcpConnection.EncodeDisable();
            }
            return true;
        }
    }
}
