using CliWrap;

namespace ExtravaCore.Commands.Framework;
public interface ICommandDriver {
        Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program);
        CommandSettings Settings { get; }

        Task<ICommandResult<TResult>> RunAsync<TResult>(
                Command command,
                Func<bool, string, TResult>? conversionDelegate = null,
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


