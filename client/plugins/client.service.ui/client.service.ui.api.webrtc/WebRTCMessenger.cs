using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using server.service.webrtc.models;

namespace client.service.ui.api.webrtc
{
    public class WebRTCMessenger : IMessenger
    {
        private readonly IClientServer clientServer;

        public WebRTCMessenger(IClientServer clientServer)
        {
            this.clientServer = clientServer;
        }

        public bool Execute(IConnection connection)
        {
            WebRTCConnectionInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<WebRTCConnectionInfo>();

            clientServer.Notify(new ClientServiceResponseInfo
            {
                Code = 0,
                Content = model,
                Path = "webrtc/execute",
                RequestId = 0
            });

            return true;
        }
    }


}
