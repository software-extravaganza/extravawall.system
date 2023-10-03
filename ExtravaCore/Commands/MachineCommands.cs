using CliWrap;
using ExtravaCore.Commands.Framework;

namespace ExtravaCore.Commands;


public class CommandMachineAll : CommandWrapperWithNoInput<CommandMachineAll, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_ALL);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

public class CommandMachineArchitecture : CommandWrapperWithNoInput<CommandMachineArchitecture, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_ARCHITECTURE);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

public class CommandMachineName : CommandWrapperWithNoInput<CommandMachineName, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_NAME);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

public class CommandMachineOs : CommandWrapperWithNoInput<CommandMachineName, string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_OS);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}
