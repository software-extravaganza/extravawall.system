using System.Reflection.Metadata.Ecma335;
using CliWrap;

// public interface IOptionCommandWith {
//     OptionSetter With();
// }

namespace ExtravaCore.Commands.Framework;

public interface IOptionSetter<TCommand, TResult> {
    IRunner<TResult> Configure(Action<TCommand> configure);
}

public interface IRunner<TResult> {
    Task<ICommandResult<TResult>> RunAsync();
}


// public class OptionSetter<TCommand, TOptions, TResult> : IOptionSetter<TCommand, TResult> where TCommand : ICommandWrapperWithOptions<TCommand, TOptions, TResult>, ICommandWrapperBase<TCommand, TResult>, new() where TOptions : new() {
//     private readonly TOptions _options;
//     private readonly TCommand _command;

//     public OptionSetter(TCommand command, TOptions options) {
//         _command = command;
//         _options = options;
//     }

//     public IRunner<TResult> Configure(Action<TCommand> configure) {
//         configure(_command);
//         return new Runner<TCommand, TResult>(_command);
//     }
// }

// public class Runner<TCommand, TResult> : IRunner<TResult> where TCommand : ICommandWrapperBase<TCommand, TResult> {
//     private readonly TCommand _command;

//     public Runner(TCommand command) {
//         _command = command;
//     }

//     public async Task<ICommandResult<TResult>> RunAsync() {
//         return await _command?.runAsync();
//     }
// }


public abstract class CommandWrapperWithOptions<TCommand, TOptions, TResult>
    : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions, TResult>,
    ICommandWithOptions<TCommand, TOptions>//,
    //ICommandWrapperWith<TCommand, TOptions>
    where TCommand : CommandWrapperWithOptions<TCommand, TOptions, TResult>, ICommandWrapperBase<TCommand, TResult>
    where TOptions : new() {
    protected TOptions _options;

    public CommandWrapperWithOptions() : base() {
        _options = new TOptions();
    }

    protected virtual async Task<ICommandResult<TResult>> runAsync() {
        var command = commandGenerator(_options);
        return await driverRunAsync(command);
    }

    protected virtual TCommand setOptions(Action<TOptions> setOptions) {
        setOptions(_options);
        return (TCommand)this;
    }

    // public OptionSetter<TCommand, TOptions, TResult> With() {
    //     return new OptionSetter<TCommand, TOptions, TResult>((TCommand)this, _options);
    // }

    protected abstract Command commandGenerator(TOptions result);
}