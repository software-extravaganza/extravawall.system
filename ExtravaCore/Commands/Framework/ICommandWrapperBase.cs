namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperBase<TCommand, out TResult>
    where TCommand : ICommandWrapperBase<TCommand, TResult>
{ }