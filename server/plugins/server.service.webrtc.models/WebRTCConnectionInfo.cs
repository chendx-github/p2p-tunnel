using MessagePack;

namespace server.service.webrtc.models
{
    [MessagePackObject]
    public class WebRTCConnectionInfo
    {
        public WebRTCConnectionInfo() { }

        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public ulong ToId { get; set; } = 0;

        [Key(3)]
        public string Data { get; set; } = string.Empty;
    }
}
