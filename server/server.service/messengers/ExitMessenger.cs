using common.libs;
using common.libs.extends;
using common.server;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers
{
    public class ExitMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ExitMessenger(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public async Task<byte[]> Execute(IConnection connection)
        {
            var res = await clientRegisterCache.Remove(connection.ConnectId).ConfigureAwait(false);
            return res ? Helper.TrueArray : Helper.FalseArray;
        }
    }
}
