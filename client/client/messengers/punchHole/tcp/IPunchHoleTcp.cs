using common.libs;
using MessagePack;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.tcp
{
    public interface IPunchHoleTcp
    {
        public SimpleSubPushHandler<ConnectParams> OnSendHandler { get; }
        public Task<ConnectResultModel> Send(ConnectParams param);

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; }
        public Task OnStep1(OnStep1Params e);

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; }
        public Task OnStep2(OnStep2Params e);

        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; }
        public Task OnStep2Retry(OnStep2RetryParams e);

        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler { get; }
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; }
        public Task OnStep2Fail(OnStep2FailParams arg);

        public Task SendStep2Stop(ulong toid);
        public Task OnStep2Stop(OnStep2StopParams e);

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; }
        public Task OnStep3(OnStep3Params arg);

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; }
        public Task OnStep4(OnStep4Params arg);
    }


    public class OnStep2RetryParams : OnStepBaseParams { }
    public class OnStep2StopParams : OnStepBaseParams { }

    [MessagePackObject]
    public class PunchHoleStep2TryInfo : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;
    }
    [MessagePackObject]
    public class PunchHoleStep2StopInfo : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(3)]
        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP;
    }

    public enum PunchHoleTcpNutssBSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_TRY = 3,
        STEP_2_FAIL = 4,
        STEP_2_STOP = 5,
        STEP_3 = 6,
        STEP_4 = 7,
        STEP_PACKET = 8,
    }

}
