namespace ExtravaCore.Commands.Framework;
// public interface ICommandWrapperWith<out TCommand, out TOptions> : ICommandWrapperBase<TCommand> where TCommand : ICommandWrapperBase<TCommand> {
//     ICommandWrapperWithOptions<TCommand, TOptions> With();
// }

public interface ICommandWrapperWithOptions<out TOptions> : ICommandWrapperBase, ICommandWithOptions<TOptions>
    where TOptions : new() {
    //TCommand SetOptions(Action<TOptions> setOptions);
}

public interface ICommandWrapperWithOptions<TResult, out TOptions> : ICommandWrapperBase<TResult>, ICommandWrapperWithOptions<TOptions>, ICommandWithOptions<TOptions>
    where TOptions : new() {

}