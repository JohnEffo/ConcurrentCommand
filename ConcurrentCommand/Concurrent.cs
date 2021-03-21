using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;

namespace ConcurrentCommand
{
   
    public  class Concurrent<TSut, TSutResult, TModel>
    {
        Func<TSut> _sutGenerator;
        int _NumberOfConccurrentClients;

        public Concurrent(Func<TSut> sutGenerator, int numberOfClients) {
            _sutGenerator = sutGenerator;
            _NumberOfConccurrentClients = numberOfClients;
        }


        /// <summary>
        /// The client and which command it is to run
        /// </summary>
        public record ClientCommand(int Client, Command<TSut, TSutResult> Command);

      
        public  Gen<List<List<ClientCommand>>> Generator(IEnumerable<Command<TSut, TSutResult>> commands)
        {
            var item =  from  client in Gen.Choose(0, _NumberOfConccurrentClients - 1)
                          from command in Gen.Elements(commands)
                          select new ClientCommand(client, command);

            const int generationSize = 6;
            var generation = from commandLength in Gen.Choose(1, generationSize)
                             from list in Gen.ListOf<Concurrent<TSut, TSutResult, TModel>.ClientCommand>(commandLength, item)
                             select list.ToList<Concurrent<TSut, TSutResult, TModel>.ClientCommand>();

            const int numberOfGenerations = 4;
            return from generationCount in Gen.Choose(1, numberOfGenerations)
                   from genList in Gen.ListOf(generationCount, generation)
                   select new List<List<ClientCommand>>(genList);
        }

        private record CommandItem(int ItemIndex, int GenerationIndex, ClientCommand Command);

        /// <summary>
        /// The result of executing the command on the client
        /// </summary>
        public record CommandResult(ClientCommand ClientCommand, TSutResult Result);

      
        public record CurrentState(bool Valid, TModel Model);
        public  IEnumerable<List<List<ClientCommand>>> Shrinker(List<List<ClientCommand>> current)
        {
            var commands = current
                .SelectMany((gen, i) => gen.Select(c => new  { GenerationId = i, Command = c })) //Number Each Gen
                .Select((c, i) => new  CommandItem(i, c.GenerationId, c.Command )) //Get commandIndex *  command * GenerationId
                .ToList();
            
            return Enumerable.Range(0, commands.Count)
                    .Select(i => commands.Where(c => c.ItemIndex != i))
                    .Select(cmds => ReAssembleCommands(cmds));
        }

        private List<List<ClientCommand>> ReAssembleCommands(IEnumerable<CommandItem> cmds)
        {
            return cmds.GroupBy(i => i.GenerationIndex)
                   .Select(gr => gr.Select(c => c.Command).ToList())
                   .ToList();
        }

        /// <summary>
        /// Test the system using the inbuilt concurrent model validator
        /// </summary>
        /// <param name="isResultValid">For result does running the command which produced the result create the same result against the model</param>
        /// <param name="commands">The list of commands which the System Under Test (SUT) can execute</param>
        /// <param name="zero">The zero or starting value for the model</param>
        /// <returns></returns>
        public Property ToProperty(Func<CommandResult, CurrentState, CurrentState> isResultValid, IEnumerable<Command<TSut, TSutResult>> commands, TModel zero)
        {
            return ToProperty(results => Validate(results, isResultValid, zero), commands);
        }

        /// <summary>
        /// Test the system using a validator function which you supply
        /// </summary>
        /// <param name="validationFunction">A function which when give the list of list of command results will return in the results were producible</param>
        /// <param name="commands">The list of commands which the System Under Test (SUT) can execute</param>
        /// <returns></returns>
        public Property ToProperty(Func<List<List<CommandResult>>, bool> validationFunction, IEnumerable<Command<TSut, TSutResult>> commands )
        {
            var arb = Arb.From(Generator(commands), Shrinker);
            return Prop.ForAll(arb,value => {
                var results = ExerciseSystem(value);
                return validationFunction(results).Label(PrettyPrintExecution(results));
            } );
        }

        private string PrettyPrintExecution(List<List<CommandResult>> results)
        {
           return Environment.NewLine + string.Join($"{Environment.NewLine}", results.Select((gen, i) => $"Generation {i}: {PrettyPrintGeneration(gen)}"));
        }

        private string PrettyPrintGeneration(List<CommandResult> gen)
        {
            return "[" + string.Join(",", gen.Select(i => $"C{i.ClientCommand.Client} {i.ClientCommand.Command} R:{i.Result}")) + "]";
        }

        private List<List<CommandResult>> ExerciseSystem(List<List<ClientCommand>> value)
        {
            var sut = _sutGenerator();
            var results = new List<List<CommandResult>>(value.Count);
            for (int v = 0; v < value.Count; v++)
            {
                results.Add(RunGeneration(value[v], sut));
            }
            return results;
        }

        private List<CommandResult> RunGeneration(List<ClientCommand> generation, TSut sut)
        {
            var clients = generation
                .GroupBy(i => i.Client)
                .ToList();
            var tasks = new Task<Collection<CommandResult>>[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                var y = i;
                tasks[y] = Task.Run(() => RunClientTasks(clients[y].Select(cmd => cmd), sut));
            }
            Task.WhenAll(tasks);

            return tasks.Select(t => t.Result)
                .SelectMany(r => r.Select(i => i))
                .ToList();
                
        }

        private  Collection<CommandResult> RunClientTasks(IEnumerable<ClientCommand> commands, TSut sut)
        {
            var results = new Collection<CommandResult>();
            foreach (var clientCommand in commands)
            {
                var(client, command) = clientCommand;
                var result = command.TargetCommand(client, sut);
                results.Add(new CommandResult(clientCommand, result.result));
            }
            return results;
        }

     

        private bool Validate(List<List<CommandResult>> results, Func<CommandResult, CurrentState, CurrentState>  validateStep, TModel zero )
        {
            var OutCome = results.Aggregate(new CurrentState(true, zero), (result, gen) => HasValidPath(gen,validateStep , result));
            return OutCome.Valid;
        }


        /// <summary>
        /// Try to find if there is a valid interleaving of results which matches the model. At any stage the commands available are the 
        /// first command found in the generation for each client. HasValidPath is recursive, the maximum stack depth is equal to the length of 
        /// the generation. The size of the search space is exponationally related to generation size, generation sizes above 20 will take may take 
        /// a long time to search.
        /// </summary>
        /// <param name="generation">The list of commands with the results which were prodcued when they were exercised</param>
        /// <param name="validateStep">The validation function which check that for a given CommandResult and State the model results match the actual command result</param>
        /// <param name="current">The current model state</param>
        /// <returns></returns>
        public CurrentState HasValidPath(List<CommandResult> generation, Func<CommandResult, CurrentState, CurrentState> validateStep, CurrentState current)
        {
            if (current.Valid)
            {
                return FindCurrentStateForValidCombinationOfCommands(generation, validateStep, current)
                    .FirstOrDefault(r => r.Valid) ?? new CurrentState(false, current.Model);
            }
            return current;
        }


        private IEnumerable<CurrentState> FindCurrentStateForValidCombinationOfCommands(List<CommandResult> gen
            , Func<CommandResult, CurrentState, CurrentState> validateStep
            , CurrentState current)
        {
            if (gen.Count == 0)
            {
               yield return current;
            }
            var clients = gen.GroupBy(c => c.ClientCommand.Client);
            foreach (var client in clients)
            {
                var currentResult = client.First();
                var nextResult = validateStep(currentResult, current);
                if (nextResult.Valid)
                {
                    var newGen = GenerateNewList(clients, client.Key);
                    foreach (var result in FindCurrentStateForValidCombinationOfCommands(newGen, validateStep, nextResult))
                    {
                        yield return result;
                    };
                }
            }
            yield break;
        }
        private List<CommandResult> GenerateNewList(IEnumerable<IGrouping<int, CommandResult>> clients, int key)
        {
            return clients.SelectMany(cl =>
                cl.Key switch
                {
                    int i when i == key => cl.Skip(1),
                    _ => cl.Select(item => item)
                }
              ).ToList();
        }
    }

}

