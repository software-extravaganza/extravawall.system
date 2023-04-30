using CliWrap;

namespace ExtravaCore.Commands;
public class UnameCommand : CommandBase<UnameCommand> {
    const string COMMAND_UNAME = "uname";
    const string UNAME_ARG_NAME = "-n";
    const string UNAME_ARG_OS = "-s";

    public UnameCommand(CommandOptions options) : base(options) {
    }

    public async Task<(bool success, string result)> GetName() {
        return await RunAsync(Cli.Wrap(COMMAND_UNAME).WithArguments(UNAME_ARG_NAME));
    }
}
