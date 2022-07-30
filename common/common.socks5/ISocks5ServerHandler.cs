using common.server;

namespace common.socks5
{
    public interface ISocks5ServerHandler
    {
        void HandleRequest(IConnection connection, Socks5Info data);
        void HandleAuth(IConnection connection, Socks5Info data);
        void HndleForward(IConnection connection, Socks5Info data);
        void HndleForwardUdp(IConnection connection, Socks5Info data);
        void HandleCommand(IConnection connection, Socks5Info data);
    }
}
