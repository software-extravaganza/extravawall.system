namespace ExtravaCore.Commands.Framework;
// public interface ICommandWrapperWith<out TCommand, out TOptions> : ICommandWrapperBase<TCommand> where TCommand : ICommandWrapperBase<TCommand> {
//     ICommandWrapperWithOptions<TCommand, TOptions> With();
// }

public interface ICommandWrapperWithOptions<out TCommand, out TOptions> : ICommandWrapperBase<TCommand>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions>
    where TOptions : new() {
    //TCommand SetOptions(Action<TOptions> setOptions);
}

public interface ICommandWrapperWithOptions<out TCommand, out TOptions, TResult> : ICommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new() {

}