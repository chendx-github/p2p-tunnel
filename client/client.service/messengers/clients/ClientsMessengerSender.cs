using common.libs;
using common.server.model;

namespace client.service.messengers.clients
{
    public class ClientsMessengerSender
    {
        public ClientsMessengerSender()
        {
        }

        public SimpleSubPushHandler<ClientsInfo> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsInfo>();

    }
}
