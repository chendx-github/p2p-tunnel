using MessagePack;

namespace server.service.manager.models
{
    [MessagePackObject]
    public class CounterResultInfo
    {
        [Key(1)]
        public int OnlineCount { get; set; }
        [Key(2)]
        public double Cpu { get; set; }
        [Key(3)]
        public double Memory { get; set; }
        [Key(4)]
        public int RunTime { get; set; }
        [Key(5)]
        public decimal TcpSendBytes { get; set; }
        [Key(6)]
        public decimal TcpReceiveBytes { get; set; }
        [Key(7)]
        public decimal UdpSendBytes { get; set; }
        [Key(8)]
        public decimal UdpReceiveBytes { get; set; }
    }
}
