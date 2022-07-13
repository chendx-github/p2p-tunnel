using client.messengers.punchHole;
using client.messengers.punchHole.udp;
using common.libs.extends;
using common.server.model;

namespace client.service.messengers.punchHole.udp
{
    public class PunchHoleUdp : IPunchHole
    {
        private readonly IPunchHoleUdp punchHoleUdp;
        public PunchHoleUdp(IPunchHoleUdp punchHoleUdp)
        {
            this.punchHoleUdp = punchHoleUdp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.UDP;

        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleUdpSteps step = (PunchHoleUdpSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleUdpSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2_1:
                    Step21(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2_Fail:
                    Step2Fail(arg);
                    break;
                case PunchHoleUdpSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleUdpSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep1(new OnStep1Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep2(new OnStep2Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step21(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep21(new OnStep21Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2Fail(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep2Fail(new OnStep2FailParams
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                //Data = arg.Data.Data.DeBytes<Step2FailModel>()
            });
        }

        private void Step3(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep3(new OnStep3Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleStep3Info>()
            });
        }
        private void Step4(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep4(new OnStep4Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleStep4Info>()
            });
        }
    }

}
