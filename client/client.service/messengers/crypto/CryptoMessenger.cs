using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;

namespace client.service.messengers.crypto
{
    public class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        private readonly IClientInfoCaching clientInfoCaching;
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, IClientInfoCaching clientInfoCaching)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.clientInfoCaching = clientInfoCaching;
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
        public bool Clear(IConnection connection)
        {
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                client.UdpConnection.EncodeDisable();
                client.TcpConnection.EncodeDisable();
            }
            return true;
        }
    }
}
