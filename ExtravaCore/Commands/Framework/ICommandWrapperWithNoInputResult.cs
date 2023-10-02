namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperWithNoInputResult<TCommand, TResult> : ICommandWrapperBase<TCommand, TResult>
    where TCommand : ICommandWrapperWithNoInputResult<TCommand, TResult>
{
    Task<ICommandResult<TResult>> RunAsync();
}
