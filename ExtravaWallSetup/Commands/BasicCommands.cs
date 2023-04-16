using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using CliWrap;
using ExtravaWallSetup.Commands.Framework;
using Semver;

namespace ExtravaWallSetup.Commands {
    public class BasicCommands : CommandBase<BasicCommands> {
        public const string COMMAND_WHICH = "which";

        public async Task<(bool success, string result)> GetProgramLocation(string program) {
            return await Instance.RunAsync(Cli.Wrap(COMMAND_WHICH).WithArguments(program));
        }

        public bool IsRunningElevated() {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isWindowsElevated = () =>
                new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(
                    WindowsBuiltInRole.Administrator
                );
            var isPosixElevated = () => Mono.Unix.Native.Syscall.geteuid() == 0;

            return isWindows ? isWindowsElevated() : isPosixElevated();
        }


        /// <summary>
        /// Checks for a supported OS to install ExtravaWall on.
        /// <code>
        /// var isSupported = new BasicCommands().IsSupportedOs();
        /// </code>
        /// </summary>
        /// <returns><typeparamref name="bool"/> value that represents a supported OS.</returns>
        public bool IsSupportedOs() {
            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            const string pattern =
                @"(?<name>[^\.]+)\.(?<version>[^-]*)-?(?<architecture>[^-]*)-?(?<qualifiers>.*)";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var osName = string.Empty;
            var osVersion = new SemVersion(0, 0);

            foreach (Match match in regex.Matches(runtimeIdentifier).Cast<Match>()) {
                if (match.Success) {
                    osName = (
                        match.Groups.ContainsKey("name") ? match.Groups["name"].Value : string.Empty
                    )
                        .ToLower();
                    osVersion = SemVersion.Parse(
                        match.Groups.ContainsKey("version")
                            ? match.Groups["version"].Value
                            : string.Empty,
                        SemVersionStyles.Any
                    );
                    break;
                }
            }

            bool isSupported = (osName, osVersion) switch {
                ("debian", { Major: > 10 }) => true,
                ("ubuntu", { Major: > 20 }) => true,
                _ => false
            };

            return isSupported;
        }
    }
}
