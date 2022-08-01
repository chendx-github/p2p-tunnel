using common.socks5;

namespace server.service.socks5
{
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        public Socks5ClientHandler()
        {
        }

        public bool HandleRequest(Socks5Info data)
        {
            return true;
        }

        public bool HandleAuth(Socks5Info data)
        {
            return true;
        }

        public bool HandleCommand(Socks5Info data)
        {
            return true;
        }

        public bool HndleForward(Socks5Info data)
        {
            return true;
        }
        public bool HndleForwardUdp(Socks5Info data)
        {
            return true;
        }

        public void Close(ulong id)
        {
        }
        public void Flush()
        {
        }

        
    }
}
