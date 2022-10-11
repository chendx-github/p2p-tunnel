using client.messengers.clients;
using client.messengers.punchHole;
using client.realize.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly IClientInfoCaching clientInfoCaching;
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerSender, IClientInfoCaching clientInfoCaching)
        {

        this.clientInfoCaching = clientInfoCaching;
            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public void Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = new PunchHoleParamsInfo();

            model.DeBytes(connection.ReceiveRequestWrap.Memory);
            //Logger.Instance.Info($"PunchHole Execute {model.ToJson()}");
            //if(model.PunchType == 3 && model.t == 1)//反向模式
            //{
            //Logger.Instance.Info($"反向模式");
            //if (clientInfoCaching.Get(model.FromId, out ClientInfo client))
            //    {
            //        if (datas.Default.canConnect.ContainsKey(client.Name))
            //        {
            //            datas.Default.canConnect[client.Name] = true;
            //        }
            //        else
            //        {
            //            datas.Default.canConnect.Add(client.Name, true);
            //        }
            //    }
               
            //}
            punchHoleMessengerSender.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }
}
