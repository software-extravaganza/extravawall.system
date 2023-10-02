using CliWrap;

namespace ExtravaCore.Commands.Framework;
public abstract class CommandWrapperBase<TCommand, TResult>
    : CommandBase, ICommandWrapperBase<TCommand, TResult>
    where TCommand : CommandWrapperBase<TCommand, TResult> {

    protected ICommandView? CommandView { get; set; }
    protected CommandOutputType? OverriddenOutputType { get; set; }


    protected async Task<ICommandResult<TCustomResult>> runCustomAsync<TCustomResult>(Command command, Func<string, TCustomResult>? conversionDelegate = null, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, conversionDelegate, customStandardOutput, customErrorOutput);
    }

    protected async Task<ICommandResult<TResult>> runAsync(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, convertResult, customStandardOutput, customErrorOutput);
    }

    protected abstract TResult convertResult(string result);

}