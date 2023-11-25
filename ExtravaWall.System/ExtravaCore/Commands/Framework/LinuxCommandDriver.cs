using CliWrap;

namespace ExtravaCore.Commands.Framework {
        public class LinuxCommandDriver : CommandDriverBase<LinuxCommandDriver>, ICommandDriver {
                public const string COMMAND_WHICH = "which";

                public LinuxCommandDriver(CommandSettings settings) : base(settings) {
                }

                public override async Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program) {
                        var command = Cli.Wrap(COMMAND_WHICH).WithArguments(program);
                        var directoryPathResponse = await RunAsync<string>(command);
                        var directory = new DirectoryInfo(directoryPathResponse.Result ?? string.Empty);
                        return directoryPathResponse.ToNewCommandResultWithValue(directory);
                }
        }
}