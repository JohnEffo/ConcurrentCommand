using Xunit;
using ConcurrentCommand;
using FsCheck;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace ConcurrentCommandTests
{
    public class ShrinkerTests
    {
        [Fact]
        public void ShinkerProperties()
        {
            var config = Configuration.QuickThrowOnFailure;
            var sut = new Concurrent<int, int, int>(() => 1, 3);
            var generator = Arb.From(sut.Generator(new Collection<ConcurrentCommand.Command<int, int>> { new IntSutIdentity() }), sut.Shrinker);
            Prop.ForAll(generator, testCase =>
            {
                var uniqeClients = MakeClientsUniqe(testCase);
                var shrunken = sut.Shrinker(uniqeClients).ToList();
                var flatClients = Flatten(uniqeClients);
                return AllShunkenClientsElementOfOriginal(shrunken, flatClients)
                .And(OnlyOneElementOfOriginalNotInShrunken(shrunken, flatClients));

            }).Check(config);
        }

        private Property OnlyOneElementOfOriginalNotInShrunken(
            IEnumerable<List<List<Concurrent<int, int, int>.ClientCommand>>> shrinkAttempts
            , List<Concurrent<int, int, int>.ClientCommand>  uniqeClients
            )
        {
            var prop = shrinkAttempts.Select(s => Flatten(s))
                            .FirstOrDefault(shrunk => uniqeClients.Count(u => shrunk.Any(s => s == u))  != uniqeClients.Count - 1);
                            
            return (prop == null)
                 .ToProperty()
                .Label($"Shunken case found where number of elements removed was not equal to 1");
        }

        private Property AllShunkenClientsElementOfOriginal(
            IEnumerable<List<List<Concurrent<int, int, int>.ClientCommand>>> shrinkAttempts
            , List<Concurrent<int, int, int>.ClientCommand>  uniqeClients
            )
        {
            return shrinkAttempts.Select(s => Flatten(s))
                .All(shunnk => shunnk.All(s => uniqeClients.Any(u => u == s)))
                .ToProperty()
                .Label("Item from shrunken list not present in original list");
        }

        private List<Concurrent<int, int, int>.ClientCommand> Flatten(List<List<Concurrent<int, int, int>.ClientCommand>> list)
        {
            return list.SelectMany(gen => gen.Select(i => i )).ToList();
        }

        private List<List<Concurrent<int, int, int>.ClientCommand>> MakeClientsUniqe(List<List<Concurrent<int, int, int>.ClientCommand>> testCase)
        {
            return testCase.SelectMany(
                    (gen, genId) => gen.Select(cmd => new { Command = cmd, Generation = genId })
                    )
                .Select((cmd, cmdId) => new { Command = cmd.Command with { Client = cmdId }, Generation = cmd.Generation })
                .GroupBy(g => g.Generation)
                .Select(gr => gr.Select(cmd => cmd.Command).ToList())
                .ToList();
        }
    }
}
