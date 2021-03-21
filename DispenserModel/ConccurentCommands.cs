using Dispenser;
using ConcurrentCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispenserModel
{
    public class GetTicketCommand : Command<ITape, int>
    {
        public override string CommandName => "GetTicket";

        public override (int, Command<ITape, int>, int) TargetCommand(int client, ITape sut)
        {
            return (client, this,  sut.GetTicket());
        }
    }

    public class ReadCommand : Command<ITape, int>
    {
        public override string CommandName => "Read";

        public override (int, Command<ITape, int>, int) TargetCommand(int client, ITape sut)
        {
            return (client, this,  sut.Read());
        }
    }
}
