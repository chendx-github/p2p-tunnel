﻿using common.libs.extends;
using common.server.model;
using System;
using System.ComponentModel;

namespace client.messengers.punchHole
{
    [Flags]
    public enum PunchHoleTypes : byte
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
        byte[] ToBytes();
    }

    public class PunchHoleStep1Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        public byte Step { get; set; } = 0;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    public class PunchHoleStep2Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        public byte Step { get; set; } = 0;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    public class PunchHoleStep2FailInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;

        public byte Step { get; set; } = 0;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    public class PunchHoleStep3Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;
        public byte Step { get; set; } = 0;
        public ulong FromId { get; set; } = 0;

        public byte[] ToBytes()
        {
            var bytes = new byte[3 + 8];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;

            var fromidBytes = FromId.ToBytes();
            Array.Copy(fromidBytes, 0, bytes, 3, fromidBytes.Length);

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];

            FromId = span.Slice(3, 8).ToUInt64();
        }

    }
    public class PunchHoleStep4Info : IPunchHoleStepInfo
    {

        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;

        public byte Step { get; set; } = 0;
        public ulong FromId { get; set; } = 0;

        public byte[] ToBytes()
        {
            var bytes = new byte[3 + 8];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;

            var fromidBytes = FromId.ToBytes();
            Array.Copy(fromidBytes, 0, bytes, 3, fromidBytes.Length);

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];

            FromId = span.Slice(3, 8).ToUInt64();
        }

    }
    public class PunchHoleReverseInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.REVERSE;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;

        public byte Step { get; set; } = 0;

        public bool TryReverse { get; set; } = false;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
                (byte)(TryReverse?1:0)
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
            TryReverse = span[3] == 1 ? true : false;
        }
    }

    public class PunchHoleResetInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.RESET;

        public PunchForwardTypes ForwardType { get; private set; } = PunchForwardTypes.FORWARD;

        public byte Step { get; set; } = 0;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
}
