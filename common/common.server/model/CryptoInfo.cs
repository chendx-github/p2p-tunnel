using common.libs;
using MessagePack;
using System;

namespace common.server.model
{
    [MessagePackObject]
    public class CryptoKeyParamsInfo
    {
        public CryptoKeyParamsInfo() { }
    }

    [MessagePackObject]
    public class CryptoSetParamsInfo
    {
        public CryptoSetParamsInfo() { }

        [Key(1)]
        public string Password { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class CryptoTestParamsInfo
    {
        public CryptoTestParamsInfo() { }

        [Key(1)]
        public byte[] Content { get; set; } = Helper.EmptyArray;
    }
}
