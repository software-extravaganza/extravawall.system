using CliWrap;

namespace ExtravaCore.Commands;


public class CommandMachineName : CommandWrapperWithNoInput<CommandMachineName, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_NAME);
    protected override string convertResult(string result) => result.Trim();
}

public class CommandMachineOs : CommandWrapperWithNoInput<CommandMachineName, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_OS);
    protected override string convertResult(string result) => result.Trim();
}
