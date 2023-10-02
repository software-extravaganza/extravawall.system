using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using CliWrap;
using Semver;
using Mono.Unix.Native;

namespace ExtravaCore.Commands;
// public class BasicCommands : CommandBase<BasicCommands> {
//     public const string COMMAND_WHICH = "which";

//     public BasicCommands(OperatingSystem os) : base(os) {
//     }

//     public async Task<ICommandResult<DirectoryInfo>> GetProgramLocation(string program) {
//         var directoryPathResponse = await RunAsync<string>(Cli.Wrap(COMMAND_WHICH).WithArguments(program));
//         var directory = new DirectoryInfo(directoryPathResponse.Result ?? string.Empty);
//         return new CommandResult<DirectoryInfo>(directory.Exists ? 0 : 1, directory, directoryPathResponse.StartTime, DateTimeOffset.Now);
//     }

//     public bool IsRunningElevated() {
//         var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
//         var isWindowsElevated = () =>
//             new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(
//                 WindowsBuiltInRole.Administrator
//             );
//         var isPosixElevated = () => Syscall.geteuid() == 0;

//         return isWindows ? isWindowsElevated() : isPosixElevated();
//     }
// }