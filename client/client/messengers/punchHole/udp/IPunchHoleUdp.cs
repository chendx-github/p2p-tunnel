using common.libs;
using MessagePack;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.udp
{
    public interface IPunchHoleUdp
    {
        public SimpleSubPushHandler<ConnectParams> OnSendHandler { get; }
        public Task<ConnectResultModel> Send(ConnectParams param);
        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; }
        public Task OnStep1(OnStep1Params arg);

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; }
        public Task OnStep2(OnStep2Params e);

        public SimpleSubPushHandler<OnStep21Params> OnStep21Handler { get; }
        public Task OnStep21(OnStep21Params e);

        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; }
        public void OnStep2Fail(OnStep2FailParams e);

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; }
        public Task OnStep3(OnStep3Params e);

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; }
        public void OnStep4(OnStep4Params arg);
    }

    [Flags]
    public enum PunchHoleUdpSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_1 = 3,
        STEP_2_Fail = 4,
        STEP_3 = 5,
        STEP_4 = 6,
    }


    [MessagePackObject]
    public class PunchHoleStep21Info : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.UDP;

        [Key(2)]
        public PunchForwardTypes ForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte Step { get; set; } = (byte)PunchHoleUdpSteps.STEP_2_1;
    }

}
