namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperBase : ICommand { }

public interface ICommandWrapperBase<TResult> : ICommandWrapperBase {

    Task<ICommandResult<TResult>> ExecuteAsync();
}