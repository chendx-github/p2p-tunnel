using common.libs;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.realize.messengers.heart
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
        public async Task<bool> Heart(IConnection connection)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                Path = "heart/Execute",
                Content = Helper.EmptyArray
            }).ConfigureAwait(false);
        }
    }
}
