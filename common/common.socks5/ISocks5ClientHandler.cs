using System;

namespace common.socks5
{
    public interface ISocks5ClientHandler
    {
        bool HandleRequest(Socks5Info data);
        bool HandleAuth(Socks5Info data);
        bool HandleCommand(Socks5Info data);
        bool HndleForward(Socks5Info data);
        void Close(ulong id);

        void Flush();
    }
}
