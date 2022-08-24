using common.libs;
using common.server;

namespace server.service.messengers
{
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public void Execute(IConnection connection)
        {
            if (connection.ServerType == common.server.model.ServerType.TCP)
            {
                Logger.Instance.Warning($"收到：{connection.ConnectId} 的心跳");
            }
        }
    }
}
