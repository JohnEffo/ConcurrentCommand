# ConcurrentCommand
A Concurrent Model Based Extension for FsCheck

In Model-based testing, we test our system against a simplified model to ensure that the system works as the model predicts. Model-based testing fits into the framework of property bases testing, where we generate test data randomly to ensure properties of our system always hold true.  Instead of creating random test data we are creating random calls against our system. This allows us to generate test scenarios which a human test writer would never conceive of writing. 

This in an implementain of model based testing which facilitates the testing of concurrent code.

For example if we have the following code:

 ```CSharp
public class TapeConcurrentBug : ITape
{
    private int _counter;

    public int GetTicket()
    {
        var result = _counter;
        Write(result + 1);
        return result + 1;
    }

    public int Read()
    {
        return _counter;
    }

    private void Write(int newValue)
    {
        _counter = newValue;
    }
}
 ```

 Which we want test for a concurrent bug, we could write the following test:


```C#
[Fact]
public void TestParrel_concurrent_buggy()
{
    var concurrentTape = new Concurrent<ITape, int, int>(() => new TapeConcurrentBug(), 2);
    concurrentTape
        .ToProperty(Validate, new Collection<ConcurrentCommand.Command<ITape, int>> { new GetTicketCommand(), new ReadCommand() }, 0)
        .VerboseCheckThrowOnFailure();
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
```

```Concurrent``` is the class which allows us test our system concurrently the line:

```var concurrentTape = new Concurrent<ITape, int, int>(() => new TapeConcurrentBug(), 2);```

This sets up a new concurrent test instance for the ITape with 2 concurrent clients. To convert the ```Concurrent``` to an FsCheck property we need to supply three items

* A validate function which can update the model depending on which command was issued and perform the test to see if the updated model matches the actual result. 
* A collection of the commands which be used against the SUT for ```TapeConcurrentBug``` the commands will be:

```C#
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
```

* The starting value for the model, in this case 0.

We can then use one of FsCheck's check methods to run the commands against the system and find our race condition.



 