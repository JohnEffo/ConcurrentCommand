using FsCheck;
using ConcurrentCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ConcurrentCommandTests
{
    public class CreateValidPathTests
    {
        private Gen<List<Concurrent<int, int, int>.CommandResult>> ParralelIntCommandGen(int listLen)
        {
            var item = from client in Gen.Choose(0, 5)
                       from result in Gen.Choose(1, 1000)
                       from command in Gen.Constant(new IntSutIdentity())
                       let clientCommand = new Concurrent<int, int, int>.ClientCommand(client, command)
                       select new Concurrent<int, int, int>.CommandResult(clientCommand, result);

            return from list in Gen.ListOf(listLen, item)
                   select list.ToList();
        }

        private Gen<List<Concurrent<bool, bool, bool>.CommandResult>> AlwaysTrueCommandResult(int listLen)
        {
            var item = from client in Gen.Choose(0, 1)
                       from result in Gen.Constant(true)
                       from command in Gen.Constant(new BoolIdentity())
                       let clientCommand = new Concurrent<bool,bool, bool>.ClientCommand(client, command)
                       select new Concurrent<bool, bool, bool>.CommandResult(clientCommand, result);

            return from list in Gen.ListOf(listLen, item)
                   select list.ToList();
        }


        [Fact]
        public void ValidPathsDoesNotCauseStackOverFlow()
        {
            var arb = Arb.From(ParralelIntCommandGen(100));

            Prop.ForAll(arb, i => GeneratePaths(i))
                .QuickCheckThrowOnFailure();
        }

        private bool GeneratePaths(List<Concurrent<int, int, int>.CommandResult> i)
        {
            var sut = new Concurrent<int, int, int>(() => 1, 5);
            var correctState = new Concurrent<int, int, int>.CurrentState(true, 1);
            return sut.HasValidPath(i, (r, s) => correctState, correctState).Valid;
        }

        /// <summary>
        /// The number of combinations for client command interleaving's is roughly C  ^ (G -1) where C is the number of clients and G is the size of the generation.
        /// For example for a generation size of 6 with 2 clients, assuming we have 3 of each client the number of combinations will be
        /// 2 * 2 * 2 * 2 * 2 as for the first operation we can select commands from either of the two clients and so on until one of the clients
        /// runs out of commands. HasValidPath does not continue to enumerate a path once the result does not match the model.
        /// This test uses a MatchTheResult which returns the Boolean supplied as the result as the current state, we then generate results 
        /// from two clients which are always true and then add another client onto the generation list which has a false result.
        /// This means the search space iAs generation ^ 2. 
        /// </summary>
        [Fact]
        public void DegenerateCaseComplates()
        {

            const int generationSize = 17;
            var gen = AlwaysTrueCommandResult(generationSize).Sample(1, 1).First();
            gen.Add(new Concurrent<bool, bool, bool>.CommandResult(new Concurrent<bool, bool, bool>.ClientCommand(2, new BoolIdentity()), false));

            var sut = new Concurrent<bool, bool, bool>(() => true, 2);
            var result = sut.HasValidPath(gen, MatchTheResult, new Concurrent<bool, bool, bool>.CurrentState(true, true));
            Assert.False(result.Valid);

        }

        private Concurrent<bool, bool, bool>.CurrentState MatchTheResult(Concurrent<bool, bool, bool>.CommandResult result, Concurrent<bool, bool, bool>.CurrentState current)
        {
            return new Concurrent<bool, bool, bool>.CurrentState(result.Result, result.Result);
        }
    }



}
