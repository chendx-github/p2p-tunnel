using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Text;
using System.Threading.Tasks;

namespace client.service.messengers.crypto
{
    public class CryptoSwap
    {
        private readonly MessengerSender messengerSender;
        private readonly ICryptoFactory cryptoFactory;

        public CryptoSwap(MessengerSender messengerSender, ICryptoFactory cryptoFactory)
        {
            this.messengerSender = messengerSender;
            this.cryptoFactory = cryptoFactory;
        }

        public async Task<ICrypto> Swap(IConnection tcp, IConnection udp)
        {
            MessageResponeInfo publicKeyResponse = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
            {
                Connection = tcp ?? udp,
                Path = "crypto/key",
                Data = Helper.EmptyArray
            }).ConfigureAwait(false);
            if (publicKeyResponse.Code != MessageResponeCodes.OK)
            {
                return null;
            }

            string publicKey = publicKeyResponse.Data.DeBytes<string>();
            IAsymmetricCrypto encoder = cryptoFactory.CreateAsymmetric(new RsaKey { PublicKey = publicKey, PrivateKey = String.Empty });
            string password = StringHelper.RandomPasswordStringMd5();
            byte[] encodedData = encoder.Encode(new CryptoSetParamsInfo { Password = password }.ToBytes());
            encoder.Dispose();

            if (tcp != null)
            {
                MessageResponeInfo setResponse = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
                {
                    Connection = tcp,
                    Path = "crypto/set",
                    Data = encodedData
                }).ConfigureAwait(false);
                if (setResponse.Code != MessageResponeCodes.OK)
                {
                    return null;
                }
            }
            if (udp != null)
            {
                MessageResponeInfo setResponse = await messengerSender.SendReply(new MessageRequestParamsInfo<byte[]>
                {
                    Connection = udp,
                    Path = "crypto/set",
                    Data = encodedData
                }).ConfigureAwait(false);
                if (setResponse.Code != MessageResponeCodes.OK)
                {
                    return null;
                }
            }

            return cryptoFactory.CreateSymmetric(password);
        }

        public async Task<bool> Test(IConnection connection)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestParamsInfo<CryptoTestParamsInfo>
            {
                Connection = connection,
                Path = "crypto/test",
                Data = new CryptoTestParamsInfo
                {
                    Content = connection.Crypto.Encode(Encoding.UTF8.GetBytes("test"))
                }
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK;
        }
    }
}
