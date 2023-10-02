using CliWrap;

namespace ExtravaCore.Commands;
public interface ICommandDriver {
        Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program);
        CommandSettings Settings { get; }

        Task<ICommandResult<TResult>> RunAsync<TResult>(
                Command command,
                Func<string, TResult>? conversionDelegate = null,
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

public class LinuxCommandDriver : CommandDriverBase<LinuxCommandDriver>, ICommandDriver {
        public const string COMMAND_WHICH = "which";

        public LinuxCommandDriver(CommandSettings settings) : base(settings) {
        }

        public override async Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program) {
                var directoryPathResponse = await RunAsync<string>(Cli.Wrap(COMMAND_WHICH).WithArguments(program));
                var directory = new DirectoryInfo(directoryPathResponse.Result ?? string.Empty);
                return new CommandResult<DirectoryInfo>(directory.Exists ? 0 : 1, directory, directoryPathResponse.StartTime, DateTimeOffset.Now);
        }
}
