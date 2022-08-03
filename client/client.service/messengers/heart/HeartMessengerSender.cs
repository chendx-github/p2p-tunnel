using common.libs;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.service.messengers.heart
{
    public class HeartMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public HeartMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 发送心跳消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task<MessageResponeInfo> Heart(IConnection connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Path = "heart/Execute",
                Content = Helper.EmptyArray
            }).ConfigureAwait(false);
        }
    }
}
