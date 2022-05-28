using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using common.libs.extends;
using common.server.model;

namespace client.service.messengers.punchHole.tcp.nutssb
{
    public class PunchHoleTcpNutssB : IPunchHole
    {
        private readonly IPunchHoleTcp punchHoleTcp;
        public PunchHoleTcpNutssB(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSB;

        public void Execute(OnPunchHoleArg arg)
        {
            if (arg.Connection.ServerType != ServerType.TCP) return;

            PunchHoleTcpNutssBSteps step = (PunchHoleTcpNutssBSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleTcpNutssBSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_TRY:
                    Step2Try(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_FAIL:
                    Step2Fail(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_STOP:
                    Step2Stop(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep1(new OnStep1Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2(new OnStep2Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2Try(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Retry(new OnStep2RetryParams
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyInfo>()
            });
        }
        private void Step2Fail(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Fail(new OnStep2FailParams
            {
                Connection = arg.Connection,
                RawData = arg.Data
            });
        }
        private void Step2Stop(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Stop(new OnStep2StopParams
            {
                Connection = arg.Connection,
                RawData = arg.Data
            });
        }

        private void Step3(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep3(new OnStep3Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleStep3Info>()
            });
        }
        private void Step4(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep4(new OnStep4Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleStep4Info>()
            });
        }
    }


}
