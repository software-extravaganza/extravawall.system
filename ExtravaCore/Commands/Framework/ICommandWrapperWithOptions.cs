namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperWithOptions<TCommand, TOptions, TResult> : ICommandWrapperBase<TCommand, TResult>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new()
{
    Task<ICommandResult<TResult>> RunAsync(TOptions? options);
}

