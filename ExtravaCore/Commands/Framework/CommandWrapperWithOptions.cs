namespace ExtravaCore.Commands.Framework
{
public abstract class CommandWrapperWithOptions<TCommand, TOptions, TResult> : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TCommand : CommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new()
{

    public virtual async Task<ICommandResult<TResult>> RunAsync(TOptions? options)
    {
        options ??= new TOptions();
        var command = commandGenerator(options);
        return await runAsync(command);
    }

    protected abstract Command commandGenerator(TOptions result);
}
}