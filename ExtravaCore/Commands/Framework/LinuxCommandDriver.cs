namespace ExtravaCore.Commands.Framework
{
        public class LinuxCommandDriver : CommandDriverBase<LinuxCommandDriver>, ICommandDriver
        {
                public const string COMMAND_WHICH = "which";

                public LinuxCommandDriver(CommandSettings settings) : base(settings)
                {
                }

                public override async Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program)
                {
                        var directoryPathResponse = await RunAsync<string>(Cli.Wrap(COMMAND_WHICH).WithArguments(program));
                        var directory = new DirectoryInfo(directoryPathResponse.Result ?? string.Empty);
                        return new CommandResult<DirectoryInfo>(directory.Exists ? 0 : 1, directory, directoryPathResponse.StartTime, DateTimeOffset.Now);
                }
        }
}