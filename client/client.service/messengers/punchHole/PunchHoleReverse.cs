using client.messengers.punchHole;

namespace client.service.messengers.punchHole
{
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
