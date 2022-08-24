using common.libs;
using common.libs.extends;
using common.server;
using server.messengers.register;

namespace server.service.messengers
{
    public class ExitMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        public ExitMessenger(IClientRegisterCaching clientRegisterCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
        }

        public void Execute(IConnection connection)
        {
            Logger.Instance.DebugDebug($"{connection.ConnectId}退出");
            connection.Disponse();

            Logger.Instance.DebugDebug(clientRegisterCaching.GetAll().ToJson());
        }
    }
}
