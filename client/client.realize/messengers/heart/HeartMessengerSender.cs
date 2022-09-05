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
        public async Task<bool> Heart(IConnection connection, IUdpServer udpServer)
        {


            Logger.Instance.Debug($"心跳发送 ping {(connection as RudpConnection).NetPeer.ConnectionState}");
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Path = "heart/Execute",
                Content = Helper.EmptyArray
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                Logger.Instance.Debug("心跳发送成功");
                udpServer.心跳发送失败次数 = 0;
                return true;
            }
            else
            {
                Logger.Instance.Debug("心跳发送失败 Disponse");
                //connection.Disponse();
                if (++udpServer.心跳发送失败次数 >= 3)
                {
                    Logger.Instance.Debug("心跳发送失败3次 Disponse");
                    udpServer.心跳发送失败次数 = 0;
                    udpServer.OnDisconnect.Push(connection);
                }
            }
            return false;
        }
    }
}
