using CliWrap;

namespace ExtravaCore.Commands;
public interface ICommandDriver {
    Task<ICommandResult> GetProgramLocationAsync(string program);

    Task<ICommandResult> RunAsync(
            Command command,
            Action<string>? customStandardOutput = null,
            Action<string>? customErrorOutput = null
    );

    Task<ICommandResult> RunAsync<TReturn>(
            Command command,
            Action<string>? customStandardOutput = null,
            Action<string>? customErrorOutput = null
    );

    Task<ICommandResultRaw> RunRawAsync(
            Command command,
            Action<string>? customStandardOutput = null,
            Action<string>? customErrorOutput = null
    );

    void SetCommandView(ICommandView view);
    void SetOutput(CommandOutputType? overriddenOutputType);
}