using common.server;
using server.messengers.register;

namespace server.service.messengers
{
    public class ExitMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ExitMessenger(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public void Execute(IConnection connection)
        {
            var res = clientRegisterCache.Remove(connection.ConnectId);
        }
    }
}
