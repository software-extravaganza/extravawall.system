namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperWithNoInputResult<out TCommand> : ICommandWrapperBase<TCommand>
    where TCommand : ICommandWrapperWithNoInputResult<TCommand> { }
public interface ICommandWrapperWithNoInputResult<out TCommand, TResult> : ICommandWrapperBase<TCommand, TResult>, ICommandWrapperWithNoInputResult<TCommand>
    where TCommand : ICommandWrapperWithNoInputResult<TCommand, TResult> {
    //Task<ICommandResult<TResult>> RunAsync();
}
