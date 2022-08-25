using common.libs;
using common.server;

namespace client.realize.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
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
