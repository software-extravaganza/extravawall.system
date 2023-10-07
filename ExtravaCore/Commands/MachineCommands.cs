using CliWrap;
using ExtravaCore.Commands.Framework;

namespace ExtravaCore.Commands;

[ExtravaCommand]
public class CommandMachineAll : CommandWrapperWithNoInput<string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_ALL);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

[ExtravaCommand]
public class CommandMachineArchitecture : CommandWrapperWithNoInput<string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_ARCHITECTURE);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

[ExtravaCommand]
public class CommandMachineName : CommandWrapperWithNoInput<string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_NAME);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

[ExtravaCommand]
public class CommandMachineOs : CommandWrapperWithNoInput<string> {
    protected override Command commandGenerator() => Cli.Wrap(CommandStrings.COMMAND_UNAME).WithArguments(CommandStrings.UNAME_ARG_OS);
    protected override string convertResult(ICommandResultRaw result) => result.StandardOutput.Trim();
}

[ExtravaCommand]
public class CommandRunningProcesses : CommandWrapperWithNoInput<List<string>> {
    protected override Command commandGenerator() => Cli.Wrap("ps").WithArguments("aux");
    protected override List<string> convertResult(ICommandResultRaw result) => result.StandardOutput.Split("\n").ToList();
}