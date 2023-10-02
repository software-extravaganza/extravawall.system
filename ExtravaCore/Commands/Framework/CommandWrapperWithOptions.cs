using System.Reflection.Metadata.Ecma335;
using CliWrap;

namespace ExtravaCore.Commands.Framework;
public abstract class CommandWrapperWithOptions<TCommand, TOptions, TResult> : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TCommand : CommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new() {
    protected TOptions _options;

    public CommandWrapperWithOptions() : base() {
        _options = new TOptions();
    }

    public virtual async Task<ICommandResult<TResult>> RunAsync() {
        var command = commandGenerator(_options);
        return await runAsync(command);
    }

    public virtual TCommand Options(Action<TOptions> setOptions) {
        setOptions(_options);
        return (TCommand)this;
    }

    protected abstract Command commandGenerator(TOptions result);
}