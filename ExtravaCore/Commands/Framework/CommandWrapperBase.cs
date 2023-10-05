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

    public abstract Task<ICommandResult<TResult>> ExecuteAsync();
}