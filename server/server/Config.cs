using System;

namespace server
{
    public class Config
    {
        public int Udp { get; set; } = 0;
        public int Tcp { get; set; } = 0;
        public int TcpBufferSize { get; set; } = 8 * 1024;
        public int TimeoutDelay { get; set; } = 20 * 1000;
        public int RegisterTimeout { get; set; } = 5000;
        
        public bool Relay { get; set; } = false;
        public string EncodePassword { get; set; } = string.Empty;

    }
}
