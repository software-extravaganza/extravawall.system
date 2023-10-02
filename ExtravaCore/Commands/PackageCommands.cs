



using System.Text.RegularExpressions;
using CliWrap;
using ExtravaCore.Commands.Framework;
using Semver;
public record CommandPackageOptions() {
    public string? Package { get; set; } = null;
}

public record OsPackage(string Name, SemVersion Version);

public class CommandPackagesInstalled : CommandWrapperWithOptions<CommandPackagesInstalled, CommandPackageOptions, IList<OsPackage>> {
    protected override Command commandGenerator(CommandPackageOptions options) {
        if (OS.Name.ToLower() == "debian" || OS.Name.ToLower() == "ubuntu") {
            return debianCommandGenerator(options);
        } else if (OS.Name.ToLower() == "centos" || OS.Name.ToLower() == "rhel" || OS.Name.ToLower() == "fedora") {
            return rpmCommandGenerator(options);
        } else {
            throw new Exception($"OS {OS.Name} is not supported.");
        }
    }

    private Command rpmCommandGenerator(CommandPackageOptions options) {
        var args = new List<string> { CommandStrings.RPM_ARG_LIST, CommandStrings.RPM_ARG_QUERY_FORMAT, CommandStrings.RPM_ARG_QUERY_FORMAT_STRING };
        if (!string.IsNullOrWhiteSpace(options.Package)) {
            args.Add("-q");
            args.Add(options.Package);
        }

        return Cli.Wrap(CommandStrings.COMMAND_RPM).WithArguments(args);
    }

    private Command debianCommandGenerator(CommandPackageOptions options) {
        var args = new List<string> { CommandStrings.DPKG_ARG_NO_PAGER, CommandStrings.DPKG_ARG_SHOW_FORMAT, CommandStrings.DPKG_ARG_SHOW_PACKAGE };
        if (!string.IsNullOrWhiteSpace(options.Package)) {
            args.Add(options.Package);
        }

        return Cli.Wrap(CommandStrings.COMMAND_DPKG_QUERY).WithArguments(args);
    }

    protected override IList<OsPackage> convertResult(bool success, string result) {
        var packages = new List<OsPackage>();
        if (success && !string.IsNullOrWhiteSpace(result)) {
            var packageLines = result.Split('\n');
            foreach (var line in packageLines.Where(l => !string.IsNullOrWhiteSpace(l))) {
                var packageAttributes = line.Split('\t').ToArray();
                if (packageAttributes.Length >= 2) {
                    var versionString = string.IsNullOrWhiteSpace(packageAttributes[1]) ? "0" : packageAttributes[1];
                    string pattern = @"^([^:\s]*:)?(?<version>([^\s\.~\+a-zA-Z]+)(\.[^\s\.~\+\-a-zA-Z]+){1,2})(?<meta>.*)";
                    // Regex tests
                    // 1.6.0-1
                    // 1:8.11 + urwcyr1.0.7~pre44 - 4.5
                    // 8:6.9.11.60 + dfsg - 1.3
                    // 9.53.3~dfsg - 7 + deb11u2
                    // 1:6.0.1r16 - 1.1
                    // 7.74.0-1.3+deb11u5
                    // 1.11.0-1
                    // 1.0.0.errata1-3
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var replaceString = @"${version}";
                    versionString = regex.Replace(versionString, replaceString);
                    versionString = versionString.Trim();
                    versionString = versionString.Last().CompareTo('+') == 0 ? versionString.Substring(0, versionString.Length - 1) : versionString;
                    SemVersion.TryParse(versionString, SemVersionStyles.Any, out var version);
                    var osPackage = new OsPackage(packageAttributes[0], version ?? new SemVersion(0));
                    packages.Add(osPackage);
                }

            }
        }
        return packages;
    }
}


//todo: bring back
// using CliWrap;
// using Semver;
// using System.Text.RegularExpressions;

// namespace ExtravaCore.Commands {
//    
//     public class GetPackages : CommandWrapper<GetPackages, List<OsPackage>> {
//         protected override List<OsPackage> ConvertResult(string result) {

//         }
//     }

//     public class PackageCommands : CommandWrapper<PackageCommands> {
//         //dpkg-query --no-pager --showformat='${Package}\t${Version}\n' -W PACKAGE


//         public PackageCommands(OperatingSystem os) : base(os) {
//         }

//         public async Task<ICommandResult<IList<OsPackage>>> PackagesInstalled(string? package = null) {
//             var packages = new List<OsPackage>();
//             var args = new List<string> { DPKG_ARG_NO_PAGER, DPKG_ARG_SHOW_FORMAT, DPKG_ARG_SHOW_PACKAGE };
//             if (!string.IsNullOrWhiteSpace(package)) {
//                 args.Add(package);
//             }

//             (var success, var result) = await RunAsync(Cli.Wrap(COMMAND_DPKG_QUERY).WithArguments(args));
//             if (success && !string.IsNullOrWhiteSpace(result)) {
//                 var packageLines = result.Split('\n');
//                 foreach (var line in packageLines.Where(l => !string.IsNullOrWhiteSpace(l))) {
//                     var packageAttributes = line.Split('\t').ToArray();
//                     if (packageAttributes.Length >= 2) {
//                         var versionString = string.IsNullOrWhiteSpace(packageAttributes[1]) ? "0" : packageAttributes[1];
//                         string pattern = @"^([^:\s]*:)?(?<version>([^\s\.~\+a-zA-Z]+)(\.[^\s\.~\+\-a-zA-Z]+){1,2})(?<meta>.*)";
//                         // Regex tests
//                         // 1.6.0-1
//                         // 1:8.11 + urwcyr1.0.7~pre44 - 4.5
//                         // 8:6.9.11.60 + dfsg - 1.3
//                         // 9.53.3~dfsg - 7 + deb11u2
//                         // 1:6.0.1r16 - 1.1
//                         // 7.74.0-1.3+deb11u5
//                         // 1.11.0-1
//                         // 1.0.0.errata1-3
//                         var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
//                         var replaceString = @"${version}";
//                         versionString = regex.Replace(versionString, replaceString);
//                         versionString = versionString.Trim();
//                         versionString = versionString.Last().CompareTo('+') == 0 ? versionString.Substring(0, versionString.Length - 1) : versionString;
//                         SemVersion.TryParse(versionString, SemVersionStyles.Any, out var version);
//                         var osPackage = new OsPackage(packageAttributes[0], version ?? new SemVersion(0));
//                         packages.Add(osPackage);
//                     }

//                 }
//             }

//             return (success, packages);
//         }

//         public record PackageCommandUpdate(bool? IsSuccessful, float PercentComplete, string? Message = null);

//         public interface IPackageCommandProgress {
//             bool IsFailed { get; }
//             PackageCommandUpdate LastUpdate { get; }

//             event EventHandler<PackageCommandUpdate>? ProgressChanged;
//         }

//         public class PackageCommandProgress : Progress<PackageCommandUpdate>, IPackageCommandProgress {
//             public static PackageCommandProgress CreateFailed(string message) {
//                 var failed = new PackageCommandProgress();
//                 failed.LastUpdate = new PackageCommandUpdate(false, 100, message);
//                 failed.IsFailed = true;
//                 return failed;
//             }

//             public bool IsFailed { get; private set; }
//             public PackageCommandUpdate LastUpdate { get; private set; } = new PackageCommandUpdate(null, 0);

//             public void SendUpdate(PackageCommandUpdate update) {
//                 OnReport(update);
//                 LastUpdate = update;
//             }
//         }

//         private (Task, IPackageCommandProgress) packageCommandWithProgress(Command command) {
//             var progress = new PackageCommandProgress();
//             var action = async () => {
//                 var outputHandler = (string o) => {
//                     string pattern = @"^\s?progress:\s*\[\s*(?<percent>[0-9]+)\s*%\s*\]\s*$";
//                     Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
//                     foreach (Match match in regex.Matches(o)) {
//                         if (match.Success && match.Groups.ContainsKey("percent")) {
//                             int.TryParse(match.Groups["percent"].Value, out int percentInt);
//                             var percent = percentInt / 100f;
//                             progress.SendUpdate(new PackageCommandUpdate(null, percent));
//                         }
//                     }
//                 };

//                 var errorHandler = (string o) => {
//                     var message = o.ToLower();
//                     if (message.Contains("unable") && message.Contains("locate")) {
//                         var packageRegex = @"package\s+(.*)";
//                         Match match = Regex.Match(message, packageRegex);
//                         if (match.Success) {
//                             string missingPackagesStrand = match.Groups[1].Value;
//                             var missingPackages = missingPackagesStrand.Split(' ');
//                             if (missingPackages.Length <= 0) {
//                                 missingPackages = new[] { missingPackagesStrand };
//                             }

//                         }
//                     }
//                     progress.SendUpdate(new PackageCommandUpdate(false, 0, o));
//                 };

//                 (var success, var result) = await RunAsync(command, outputHandler, errorHandler);
//                 progress.SendUpdate(new PackageCommandUpdate(success, 1));
//             };

//             var task = Task.Run(action);
//             return (task, progress);
//         }

//         public (Task, IPackageCommandProgress) InstallPackage(params string[] packages) {
//             var scubbedPackages = packages.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
//             if (scubbedPackages.Count <= 0) {
//                 return (Task.CompletedTask, PackageCommandProgress.CreateFailed("No package was provided."));
//             }

//             var args = new List<string> { APTGET_ARG_ASSUME_YES, APTGET_ARG_SHOW_PROGRESS, APTGET_ARG_INSTALL };
//             args.AddRange(scubbedPackages);

//             return packageCommandWithProgress(Cli.Wrap(COMMAND_APTGET).WithArguments(args));
//         }

//         public async Task<bool> IsPackageInstalled(string package) {
//             return await IsPackageInstalledWithAtLeastVersion(package, new SemVersion(0));
//         }

//         public async Task<(bool, SemVersion)> IsPackageInstalledIncludeVersion(string package) {
//             return await IsPackageInstalledIncludeVersionWithAtLeastVersion(package, new SemVersion(0));
//         }

//         public async Task<(bool, SemVersion)> IsPackageInstalledIncludeVersionWithAtLeastVersion(string package, SemVersion minVersion) {
//             (var success, var packages) = await PackagesInstalled(package);
//             return (success, packages.FirstOrDefault(p => versionIsAtLeast(minVersion, p.Version))?.Version ?? new SemVersion(0));
//         }

//         public async Task<bool> IsPackageInstalledWithAtLeastVersion(string package, SemVersion minVersion) {
//             (var success, var packages) = await PackagesInstalled(package);
//             return success && packages.Any(p => versionIsAtLeast(minVersion, p.Version));
//         }

//         private static bool versionIsAtLeast(SemVersion minVersion, SemVersion verionToCheck) {
//             return minVersion.WithoutPrereleaseOrMetadata().ComparePrecedenceTo(verionToCheck.WithoutPrereleaseOrMetadata()) <= 0;
//         }
//     }
// }