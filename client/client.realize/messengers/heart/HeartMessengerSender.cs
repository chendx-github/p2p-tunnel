using common.libs;
using common.server;
using common.server.model;
using LiteNetLib;
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
            

                Logger.Instance.Debug($"心跳发送 ping {(connection as RudpConnection).NetPeer.ConnectionState}");
            bool flag1 = await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                Path = "heart/Execute",
                Content = Helper.EmptyArray
            }).ConfigureAwait(false);
            if(!flag1)
            {
                Logger.Instance.Debug("心跳发送失败 Disponse");
                connection.Disponse();
            }
            else
            {
                Logger.Instance.Debug("心跳发送成功");

            }
            return flag1;
        }
    }
}
