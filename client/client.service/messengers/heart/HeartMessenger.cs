using common.libs;
using common.server;

namespace client.service.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public byte[] Execute(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
