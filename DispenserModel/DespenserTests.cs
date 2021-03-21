using Dispenser;
using FsCheck;
using ConcurrentCommand;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace DispenserModel
{
    public class DespenserTests
    {
        [Fact]
        public void TestSerial_withSpec()
        {
            new SerialSpec(() => new TapeSerialBug())
                    .ToProperty()
                    .QuickCheckThrowOnFailure();
        }


        [Fact]
        public void TestParrel_withSpec()
        {
            new SerialSpec(() => new TapeConcurrentBug())
                    .ToProperty()
                    .VerboseCheckThrowOnFailure();
        }

        [Fact]
        public void TestParrel_concurrent_buggy()
        {
            var concurrentTape = new Concurrent<ITape, int, int>(() => new TapeConcurrentBug(), 2);
            concurrentTape
                .ToProperty(Validate, new Collection<ConcurrentCommand.Command<ITape, int>> { new GetTicketCommand(), new ReadCommand() }, 0)
                .VerboseCheckThrowOnFailure();

        }

        [Fact]
        public void TestParrel_concurrent_buggy_std()
        {
            var t = Configuration.QuickThrowOnFailure;
            t.Replay = FsCheck.Random.StdGen.NewStdGen(1867961639, 296867728);
            var concurrentTape = new Concurrent<ITape, int, int>(() => new TapeConcurrentBug(), 2);
            concurrentTape
                .ToProperty(Validate, new Collection<ConcurrentCommand.Command<ITape, int>> { new GetTicketCommand(), new ReadCommand() }, 0)
                .Check(t);
                

        }

        private Concurrent<ITape, int, int>.CurrentState Validate(Concurrent<ITape, int, int>.CommandResult cmdResult, Concurrent<ITape, int, int>.CurrentState state)
        {
            var model = state.Model;
            var newModel = cmdResult.ClientCommand.Command switch
            {
                GetTicketCommand _ => model + 1,
                ReadCommand _ => model,
                _ => throw new Exception("Bad command processed")
            };
            return new Concurrent<ITape, int, int>.CurrentState(cmdResult.Result == newModel, newModel);
        }

        [Fact]
        public void TestParrel_concurrent_locked()
        {
            var concurrentTape = new Concurrent<ITape, int, int>(() => new TapeConcurrentLocking(), 2);
            concurrentTape
                .ToProperty(Validate, new Collection<ConcurrentCommand.Command<ITape, int>> { new GetTicketCommand(), new ReadCommand() }, 0)
                .VerboseCheckThrowOnFailure();

        }

       
    }
}
