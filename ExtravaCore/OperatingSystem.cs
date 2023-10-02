using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CliWrap;
using ExtravaCore.Commands;
using ExtravaCore.Commands.Framework;
using Semver;

namespace ExtravaCore;

public class OperatingSystem : IOperatingSystem {
    public string Name { get; private init; }
    public SemVersion Version { get; private init; }
    public Func<ICommandDriver> CommandDriverFactory { get; private init; }

    public OperatingSystem(CommandSettings commandSettings, CommandServiceProvider commandServiceProvider) {
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

        Func<ICommandDriver> commandDriverFactory = (osName, osVersion) switch {
            ("debian", { Major: > 10 }) => () => commandServiceProvider.GetService<LinuxCommandDriver>(),
            ("ubuntu", { Major: > 20 }) => () => commandServiceProvider.GetService<LinuxCommandDriver>(),
            ("fedora", { Major: > 37 }) => () => commandServiceProvider.GetService<LinuxCommandDriver>(),
            _ => throw new Exception("Unsupported OS")
        };

        CommandDriverFactory = commandDriverFactory;
        Name = osName;
        Version = osVersion;
    }

    // /// <summary>
    // /// Checks for a supported OS to install ExtravaWall on.
    // /// <code>
    // /// var isSupported = new BasicCommands().IsSupportedOs();
    // /// </code>
    // /// </summary>
    // /// <returns><typeparamref name="bool"/> value that represents a supported OS.</returns>
    // public bool IsSupportedOs() {
    //     bool isSupported = (osName, osVersion) switch {
    //         ("debian", { Major: > 10 }) => true,
    //         ("ubuntu", { Major: > 20 }) => true,
    //         _ => false
    //     };

    //     return isSupported;
    // }
}

public class CommandDriverFactory {
}