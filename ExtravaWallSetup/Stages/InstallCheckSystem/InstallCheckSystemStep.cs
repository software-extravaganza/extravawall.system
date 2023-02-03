using ExtravaWallSetup.Commands;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using Hardware.Info;
using Humanizer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static ExtravaWallSetup.Stages.InstallCheckSystem.InstallCheckSystemStep;
using v = Semver.SemVersion;

namespace ExtravaWallSetup.Stages.InstallCheckSystem {
    public class InstallCheckSystemStep : StepBase {

        public override string Name => "Install Starting...";

        public override StageType Stage => StageType.InstallCheckSystem;

        public override short StepOrder => 0;

        public override bool AutoComplete => true;

        protected override async Task Complete() {

            await Task.CompletedTask;
        }

        private const string SUCCESS_MARK = ""; //"✔";
        private const string FAIL_MARK = ""; //"❌";
        private const string DECLARE_SUCCESS = $"{SUCCESS_MARK} Success";
        private const string DECLARE_FAILED = $"{FAIL_MARK} Failed";
        private const string DECLARE_NOT_FOUND = $"{FAIL_MARK} Not Found (will install)";

        protected override async Task Execute() {
            var success = await installerHeaders(0, "Checking basics", CheckBasics)
            && await installerHeaders(1, "Looking for system tools & commands", async (w) => await CheckForCommands(w, "which", "uname", "apt-get"))
            && await installerHeaders(1, "Setting up packages", SetupRequiredPackages);

            if (!success) {
                // setup failed
                return;
            }

            //setup succeeded
            await Task.CompletedTask;
        }

        public abstract record BasePackage(string Name, v Version) {
            public abstract bool ShouldMarkToInstall { get; }

        }
        public record PackageDependency(string Name, v Version) : BasePackage(Name, Version) {
            public override bool ShouldMarkToInstall => false;
        }

        public record PackageNeeded(string Name, v Version, params PackageDependency[] Dependencies) : BasePackage(Name, Version) {
            public override bool ShouldMarkToInstall => true;

        }

        public class PackageNeededCollection : List<PackageNeeded> {
           
            public PackageNeededCollection(params PackageNeeded[] dependencies) {
                AddRange(dependencies);
            }

            public IEnumerable<BasePackage> AllPackagesToCheck() {
                foreach(var neededPackage in this) {
                    yield return neededPackage;
                    foreach(var dependencyPackage in neededPackage.Dependencies) {
                        yield return dependencyPackage;
                    }
                }
            }
        }

        private async Task<bool> SetupRequiredPackages(ITextOutput writer) {
            PackageCommands.IPackageCommandProgress? installUpdate = null;
            Task? installTask = null;
            var packagesThatAreNeeded = new PackageNeededCollection(
                new PackageNeeded("curl", new v(7, 74),
                    new PackageDependency("chafa", new v(1, 6))
                ),
                new PackageNeeded("neofetch", new v(7, 1),
                   new PackageDependency( "libmagickwand-6.q16-6", new v(6,9) ),
                    new PackageDependency( "ghostscript", new v(9, 53) ),
                    new PackageDependency( "libgs9", new v(9, 53) ),
                    new PackageDependency( "libfontconfig1", new v(2, 13) ),
                    new PackageDependency( "fontconfig-config", new v(2, 13) ),
                    new PackageDependency( "fonts-dejavu-core", new v(2,37) ),
                    new PackageDependency( "fonts-droid-fallback", new v(6,0) ),
                    new PackageDependency( "fonts-noto-mono", new v(20201225) ),
                    new PackageDependency( "libgs9-common", new v(9, 53) ),
                    new PackageDependency( "fonts-urw-base35", new v(20200910) ),
                    new PackageDependency( "gsfonts", new v(8, 11) ),
                    new PackageDependency( "imagemagick-6-common", new v(1,11) ),
                    new PackageDependency( "libheif1", new v(1, 11) ),
                    new PackageDependency( "libaom0", new v(1) ),
                    new PackageDependency( "libcups2", new v(2,3) ),
                    new PackageDependency( "libavahi-client3", new v(0,8) ),
                    new PackageDependency( "libavahi-common3", new v(0,8) ),
                    new PackageDependency( "libavahi-common-data", new v(0,8) ),
                    new PackageDependency( "libcurl4", new v(7,74) ),
                    new PackageDependency( "libfreetype6", new v(2,10) ),
                    new PackageDependency( "libbrotli1", new v(1) ),
                    new PackageDependency( "libchafa0", new v(1,6) ),
                    new PackageDependency( "libdav1d4", new v(0,7) ),
                    new PackageDependency( "libde265-0", new v(1,0) ),
                    new PackageDependency( "libtiff5", new v(4,2) ),
                    new PackageDependency( "libdeflate0", new v(1,7) ),
                    new PackageDependency( "libfftw3-double3", new v(3,3) ),
                    new PackageDependency( "shared-mime-info", new v(2,0) ),
                    new PackageDependency( "liblqr-1-0", new v(0,4) ),
                    new PackageDependency( "libglib2.0-0", new v(2,66) ),
                    new PackageDependency( "libglib2.0-data", new v(2,66) ),
                    new PackageDependency( "libgomp1", new v(10,2) ),
                    new PackageDependency( "libidn11", new v(1,33) ),
                    new PackageDependency( "libijs-0.35", new v(0,35) ),
                    new PackageDependency( "libjbig0", new v(2,1) ),
                    new PackageDependency( "libjbig2dec0", new v(0,19) ),
                    new PackageDependency( "libjpeg62-turbo", new v(2,0) ),
                    new PackageDependency( "liblcms2-2", new v(2,12) ),
                    new PackageDependency( "libldap-2.4-2", new v(2,4) ),
                    new PackageDependency( "libldap-common", new v(2,4) ),
                    new PackageDependency( "libnghttp2-14", new v(1,43) ),
                    new PackageDependency( "libx265-192", new v(3,4) ),
                    new PackageDependency( "libnuma1", new v(2,0) ),
                    new PackageDependency( "libopenjp2-7", new v(2,4) ),
                    new PackageDependency( "libpaper-utils", new v(1,1) ),
                    new PackageDependency( "libpaper1", new v(1,1) ),
                    new PackageDependency( "libpng16-16", new v(1,6) ),
                    new PackageDependency( "librtmp1", new v(2,4) ),
                    new PackageDependency( "libsasl2-2", new v(2,1) ),
                    new PackageDependency( "libsasl2-modules", new v(2,1) ),
                    new PackageDependency( "libsasl2-modules-db", new v(2,1) ),
                    new PackageDependency( "libssh2-1", new v(1,9) ),
                    new PackageDependency( "libwebpmux3", new v(0,6) ),
                    new PackageDependency( "libwebp6", new v(0,6) ),
                    new PackageDependency( "libxml2", new v(2,9) ),
                    new PackageDependency( "poppler-data", new v(0,4) ),
                    new PackageDependency( "xdg-user-dirs", new v(0,17) )
                )
            );

            var packagesNotInsalled = new List<BasePackage>();
            foreach (var package in packagesThatAreNeeded.AllPackagesToCheck()) {
                writer.WriteLine($"Looking for installed package '{package.Name}' (>={package.Version})...");
                var isInstalled = false;
                await Console.CommandAsync<PackageCommands>(async (cmd, output) => {
                    isInstalled = await cmd.IsPackageInstalledWithAtLeastVersion(package.Name, package.Version);
                });

                if (isInstalled) {
                    writer.Write($"\t{DECLARE_SUCCESS}");
                }
                else {
                    writer.Write($"\t{DECLARE_NOT_FOUND}");
                    packagesNotInsalled.Add(package);
                }

            }

            var packagesToInstallVerbose = packagesNotInsalled.Select(p => p.Name).ToArray();
            var packagesToInstall = packagesNotInsalled.Where(p=> p.ShouldMarkToInstall).Select(p => p.Name).ToArray();
            if (packagesToInstall.Length <= 0) {
                writer.WriteLine($"All required packages were already installed!\n - {string.Join(',', packagesToInstallVerbose)}");
            }
            else {
                writer.WriteLine();
                writer.WriteLine($"Installing all packages that were not found\n - {string.Join(',', packagesToInstallVerbose)}");
                await Console.CommandAsync<PackageCommands>(async (cmd, output) => {
                    (installTask, installUpdate) = cmd.InstallPackage(packagesToInstall);
                });
            }

            if (installTask is null || (installUpdate?.IsFailed ?? true)) {
                // todo: write fail state for progress bar
                return false;
            }
            else {
                var progress = Console.GetNewProgressBar(0, "Installing...");
                installUpdate.ProgressChanged += (sender, update) => {
                    progress.UpdateProgress(update.PercentComplete);
                    if (!(update?.IsSuccessful ?? false)) {
                        progress.Failed(update.Message);
                    }
                };
                await installTask;
            }

            return true;
        }

        private async Task<T> installerHeaders<T>(int stage, string shortDescription, Func<ITextOutput, Task<T>> action) where T : notnull {
            const int titleSpace = 45;
            var headPadSize = (int)Math.Floor((titleSpace - shortDescription.Length) / 2m);
            var headPaddingLeft = new string(' ', headPadSize);
            var headPaddingRight = new string(' ', titleSpace - (shortDescription.Length + headPadSize));
            var hwriter = Console.GetNewWriter(Color.White);
            hwriter.WriteLine();
            hwriter.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            hwriter.WriteLine($">>>>>>>>>>>>>>>>>>  Stage {stage:00}  >>>>>>>>>>>>>>>>>>>");
            hwriter.WriteLine($">>{headPaddingLeft}{shortDescription}{headPaddingRight}>>");
            hwriter.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            var awriter = Console.GetNewWriter();
            var startTime = DateTime.Now;
            var result = await (action?.Invoke(awriter) ?? Task.FromResult(default(T)));
            var endDuration = DateTime.Now - startTime;
            var footerDescription = $"Took {endDuration.Humanize()}";
            var footPadSize = titleSpace - (footerDescription.Length + 1);
            var footPaddingRight = new string(' ', footPadSize);
            var fwriter = Console.GetNewWriter(Color.Gray);
            fwriter.WriteLine($"<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            fwriter.WriteLine($"<< {footerDescription}{footPaddingRight}<<");
            fwriter.WriteLine($"<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            return result;
        }

        private async Task<bool> CheckForCommands(ITextOutput writer, params string[] commands) {
            foreach (var command in commands) {
                var success = await CheckForCommand(writer, command);
                if (!success) {
                    return false;
                }
            }

            return true;
        }
        private async Task<bool> CheckForCommand(ITextOutput writer, string command) {
            writer.WriteLine($"Checking for '{command}' command...");
            var commandIsFound = false;
            await Console.CommandAsync<BasicCommands>(async (cmd, output) => {
                (var success, var result) = await cmd.GetProgramLocation(command);
                commandIsFound = success && !string.IsNullOrWhiteSpace(result);
            });

            if (commandIsFound) {
                writer.Write($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.Write($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep($"Installation requires the '{command}' command.");
            }

            return commandIsFound;
        }

        private async Task<bool> CheckBasics(ITextOutput writer) {
            writer.WriteLine("Checking for supported OS/version...");

            var isSupportedOs = false;
            await Console.CommandAsync<BasicCommands>((cmd, output) => {
                isSupportedOs = cmd.IsSupportedOs();
            });

            if (isSupportedOs) {
                writer.Write($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.Write($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep("Installation requires an OS that is supported.");
                return false;
            }

            writer.WriteLine("Checking for elevated permissions...");
            var isElevated = false;
            await Console.CommandAsync<BasicCommands>((cmd, output) => {
                isElevated = cmd.IsRunningElevated();
            });

            if (isElevated) {
                writer.Write($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.Write($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep("Installation requires an ELEVATED execution (sudo make me a sandwich).");
                return false;
            }

            return true;
        }
    }
}
