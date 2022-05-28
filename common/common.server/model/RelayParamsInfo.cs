using common.libs;
using MessagePack;
using System;

namespace common.server.model
{
    /// <summary>
    /// 中继
    /// </summary>
    [MessagePackObject]
    public class RelayParamsInfo
    {
        public RelayParamsInfo() { }

        [Key(1)]
        public ulong ToId { get; set; } = 0;

        [Key(2)]
        public Memory<byte> Path { get; set; }

        [Key(3)]
        public byte[] Data { get; set; } = Helper.EmptyArray;
    }
}
