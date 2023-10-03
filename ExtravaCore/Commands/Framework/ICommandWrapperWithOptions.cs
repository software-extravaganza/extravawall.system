namespace ExtravaCore.Commands.Framework;

public interface ICommandWrapperWithOptions<out TCommand, out TOptions> : ICommandWrapperBase<TCommand>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions>
    where TOptions : new() {
    TCommand Options(Action<TOptions> setOptions);
}

public interface ICommandWrapperWithOptions<out TCommand, out TOptions, TResult> : ICommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new() {
    Task<ICommandResult<TResult>> RunAsync();
}