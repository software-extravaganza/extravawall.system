using ExtravaWallSetup.Commands;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using Hardware.Info;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup.Stages.InstallCheckSystem {
    public class InstallCheckSystemStep : StepBase {

        public override string Name => "Install Starting...";

        public override StageType Stage => StageType.InstallCheckSystem;

        public override short StepOrder => 0;

        public override bool AutoComplete => true;

        protected override async Task Complete() {

            await Task.CompletedTask;
        }

        private const string SUCCESS_MARK = "✔";
        private const string FAIL_MARK = "❌";
        private const string DECLARE_SUCCESS = $"{SUCCESS_MARK} Success";
        private const string DECLARE_FAILED = $"{FAIL_MARK} Failed";

        protected override async Task Execute() {
            var success = await installerHeaders(0, "Checking basics", CheckBasics);
            if (!success) {
                return;
            }

            success = await installerHeaders(1, "Looking for standard tools & commands", async (w) => {
                return await CheckForCommands(w, "which", "uname", "apt-get", "bob");
            });

            if (!success) {
                return;
            }

           

            await Console.CommandAsync<PackageCommands>(async (cmd, output) => {
                (var success, var result) = await cmd.ListPackages("curl");
                if (!success) {
                    output.WriteStandardLine("apt curl fail");
                }
                else {
                    output.WriteStandardLine("apt curl success");
                }

                (var success2, var result2) = await cmd.ListPackages("blah");
                if (!success2) {
                    output.WriteStandardLine("apt blah fail");
                }
                else {
                    output.WriteStandardLine("apt blah success");
                }
            });
            await Task.CompletedTask;
        }

        private async Task<T> installerHeaders<T>(int stage, string shortDescription, Func<ITextOutput, Task<T>> action) where T: notnull {
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
                if(!success) {
                    return false;
                }
            }

            return true;
        }
        private async Task<bool> CheckForCommand(ITextOutput writer, string command) {
            writer.Write($"Checking for '{command}' command...");
            var commandIsFound = false;
            await Console.CommandAsync<BasicCommands>(async (cmd, output) => {
                (var success, var result) = await cmd.GetProgramLocation(command);
                commandIsFound = success && !string.IsNullOrWhiteSpace(result);
            });

            if (commandIsFound) {
                writer.WriteLine($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.WriteLine($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep($"Installation requires the '{command}' command.");
            }

            return commandIsFound;
        }

        private async Task<bool> CheckBasics(ITextOutput writer) {
            writer.Write("Checking for supported OS/version...");

            var isSupportedOs = false;
            await Console.CommandAsync<BasicCommands>((cmd, output) => {
                isSupportedOs = cmd.IsSupportedOs();
            });

            if (isSupportedOs) {
                writer.WriteLine($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.WriteLine($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep("Installation requires an OS that is supported.");
                return false;
            }

            writer.Write("Checking for elevated permissions...");
            var isElevated = false;
            await Console.CommandAsync<BasicCommands>((cmd, output) => {
                isElevated = cmd.IsRunningElevated();
            });

            if (isElevated) {
                writer.WriteLine($"\t{DECLARE_SUCCESS}");
            }
            else {
                writer.WriteLine($"\t{DECLARE_FAILED}");
                Install.RequestEndOnNextStep("Installation requires an ELEVATED execution (sudo make me a sandwich).");
                return false;
            }

            return true;
        }
    }
}
