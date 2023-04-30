using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using CliWrap;
using Semver;
using Mono.Unix.Native;

namespace ExtravaCore.Commands;
public class BasicCommands : CommandBase<BasicCommands> {
    public const string COMMAND_WHICH = "which";

    public BasicCommands(CommandOptions options) : base(options) {
    }

    public async Task<(bool success, string result)> GetProgramLocation(string program) {
        return await RunAsync(Cli.Wrap(COMMAND_WHICH).WithArguments(program));
    }

    public bool IsRunningElevated() {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isWindowsElevated = () =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(
                WindowsBuiltInRole.Administrator
            );
        var isPosixElevated = () => Syscall.geteuid() == 0;

        return isWindows ? isWindowsElevated() : isPosixElevated();
    }
}