using common.server.model;
using MessagePack;
using System;
using System.ComponentModel;

namespace client.messengers.punchHole
{
    [Flags]
    public enum PunchHoleTypes
    {
        [Description("UDP打洞")]
        UDP,
        [Description("IP欺骗打洞")]
        TCP_NUTSSA,
        [Description("端口复用打洞")]
        TCP_NUTSSB,
        [Description("反向链接")]
        REVERSE,
        [Description("重启")]
        RESET,
    }


    public interface IPunchHoleStepInfo
    {
        PunchHoleTypes PunchType { get; }
        public PunchForwardTypes ForwardType { get; }
        public byte Step { get; set; }
    }

    [MessagePackObject]
    public class PunchHoleStep1Info : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte Step { get; set; } = 0;
    }
    [MessagePackObject]
    public class PunchHoleStep2Info : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte Step { get; set; } = 0;
    }
    [MessagePackObject]
    public class PunchHoleStep2FailInfo : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = 0;
    }
    [MessagePackObject]
    public class PunchHoleStep3Info : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = 0;

        [Key(4)]
        public ulong FromId { get; set; } = 0;

    }
    [MessagePackObject]
    public class PunchHoleStep4Info : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = 0;

        [Key(4)]
        public ulong FromId { get; set; } = 0;

    }
    [MessagePackObject]
    public class PunchHoleReverseInfo: IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.REVERSE;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = 0;

        [Key(4)]
        public bool TryReverse { get; set; } = false;
    }

    [MessagePackObject]
    public class PunchHoleResetInfo : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.RESET;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = 0;
    }
}
