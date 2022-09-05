using client.messengers.punchHole;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 反向打洞
    /// </summary>
    public class PunchHoleReverse : IPunchHole
    {
        private readonly PunchHoleMessengerSender  punchHoleMessengerSender;
        public PunchHoleReverse(PunchHoleMessengerSender punchHoleMessengerSender)
        {

            this.punchHoleMessengerSender = punchHoleMessengerSender;
        }

        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;

        public void Execute(OnPunchHoleArg arg)
        {
            punchHoleMessengerSender.OnReverse.Push(arg);
        }
    }
}
