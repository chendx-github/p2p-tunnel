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
        }
        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
