namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperBase<out TCommand> : ICommand
    where TCommand : ICommandWrapperBase<TCommand> { }

public interface ICommandWrapperBase<out TCommand, TResult> : ICommandWrapperBase<TCommand>
    where TCommand : ICommandWrapperBase<TCommand, TResult> { }