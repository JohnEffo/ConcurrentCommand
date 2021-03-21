namespace ConcurrentCommand
{
    public abstract class Command<TSut, TSutResult>
    {
        public abstract string CommandName { get; }
        public override string ToString() => CommandName;
        public abstract (int client , Command<TSut, TSutResult> command, TSutResult result) TargetCommand(int client, TSut sut);

    }

}

