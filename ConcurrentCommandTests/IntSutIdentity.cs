using ConcurrentCommand;

namespace ConcurrentCommandTests
{
    public class IntSutIdentity : ConcurrentCommand.Command<int, int>
    {
        public override string CommandName => "IntIdentity";

        public override (int client, ConcurrentCommand.Command<int, int> command, int result) TargetCommand(int client, int sut)
        {
            return (client, this, sut);
        }

    }



    public class BoolIdentity : ConcurrentCommand.Command<bool,bool>
    {
        public override string CommandName => "BoolIdentity";

        public override (int client, Command<bool, bool> command, bool result) TargetCommand(int client, bool sut)
        {
            return (client, this, sut);
        }
    }
}
