using common.libs;
using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5ServerHandler: ISocks5ServerHandler
    {

    }
    public class VeaSocks5ServerHandler : Socks5ServerHandler, IVeaSocks5ServerHandler
    {
        public VeaSocks5ServerHandler(IVeaSocks5MessengerSender socks5MessengerSender,common.socks5.Config config, WheelTimer<object> wheelTimer)
            :base(socks5MessengerSender, config, wheelTimer)
        {
        }
    }

}
