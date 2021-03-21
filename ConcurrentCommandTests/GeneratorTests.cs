using System;
using Xunit;
using ConcurrentCommand;
using FsCheck;
using System.Collections.ObjectModel;
using System.Linq;

namespace ConcurrentCommandTests
{
    public class GeneratorTests
    {
        [Fact]
        public void GeneratorProducesCommandsFromTheCorrectNumberOfClients()
        {
            Prop.ForAll(Arb.Default.PositiveInt(), clients =>
             {
                 var clientsVal = clients.Item;
                 var sut = new Concurrent<int, int, int>(() => 1, clientsVal);
                 var generator = sut.Generator(new Collection<ConcurrentCommand.Command<int, int>> { new IntSutIdentity() }).ToArbitrary();
                 return Prop.ForAll(generator,
                     gens =>
                     {
                         var clients = gens
                             .SelectMany(gen => gen.Select(i => i))
                             .GroupBy(i => i.Client);
                             
                         return (clients.All(gr => gr.Key >=0 && gr.Key < clientsVal) && clients.Any())
                         .ToProperty()
                         .Label($"Maximum number of clients:{clientsVal} actual number of client found in commands:{clients.Count()}");
                     }
                 ) ;
             }).QuickCheckThrowOnFailure();
        }
    }
}
