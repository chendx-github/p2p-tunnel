using MessagePack;

namespace common.server.model
{
    [MessagePackObject]
    public class CommonResponseInfo<T>
    {
        [Key(1)]
        public int Code { get; set; } = 0;
        [Key(2)]
        public string Msg { get; set; } = string.Empty;
        [Key(3)]
        public T Data { get; set; } = default;
    }
}
