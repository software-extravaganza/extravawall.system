using CliWrap;

namespace ExtravaCore.Commands.Framework;
public abstract class CommandWrapperBase<TResult>
    : CommandBase, ICommandWrapperBase<TResult> {

    protected ICommandView? CommandView { get; set; }
    protected CommandOutputType? OverriddenOutputType { get; set; }


    protected async Task<ICommandResult<TCustomResult>> driverRunCustomAsync<TCustomResult>(Command command, Func<ICommandResultRaw, TCustomResult>? conversionDelegate = null, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, conversionDelegate, customStandardOutput, customErrorOutput);
    }

    protected async Task<ICommandResult<TResult>> driverRunAsync(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, convertResult, customStandardOutput, customErrorOutput);
    }

    protected abstract TResult convertResult(ICommandResultRaw result);
    //todo: finish this
    // {
    //     return result switch {
    //         ICommandResultRaw<TResult> r => r.Result,
    //         _ => throw new InvalidCastException($"Cannot convert {result.GetType()} to {typeof(TResult)}")
    //     };
    // }

    public abstract Task<ICommandResult<TResult>> ExecuteAsync();
}