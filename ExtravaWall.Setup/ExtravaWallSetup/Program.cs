// See https://aka.ms/new-console-template for more information
using CliWrap;
using ExtravaWallSetup;
using Terminal.Gui;
using Hardware.Info;
using Spectre.Console;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Tomlyn;
using static System.Net.Mime.MediaTypeNames;
using ReactiveUI;
using System.Reactive.Concurrency;
using ExtravaWallSetup.GUI.Framework;
await new ExtravaServiceProvider().GetService<InstallBooter>().Run(args);

public partial class Program {

    InstallManager _installManager;

    public Program(InstallManager installManager) {
        _installManager = installManager;
    }

    public void Run(InstallBooter booter, string[] args) {

        try {

            //var rule = new Rule($"[red]ExtravaWall Installer ({RuntimeInformation.OSArchitecture})[/]");
            //AnsiConsole.Write(rule);

            //var layout = new Layout("Root")
            //.SplitColumns(
            //    new Layout("left"),
            //    new Layout("right")
            //        .SplitRows(
            //            new Layout("top"),
            //            new Layout("bottom")));

            // var priviledgeManager = new PriviledgeManager();
            // var result = priviledgeManager.ExtractHelper();
            //
            try {
                // var priviledgeManager = new PriviledgeManager();
                // var result = priviledgeManager.ExtractHelper();
                //
                Terminal.Gui.Application.Init();
                Terminal.Gui.Application.Driver.ULCorner = '\u256D';
                Terminal.Gui.Application.Driver.URCorner = '\u256E';
                Terminal.Gui.Application.Driver.LLCorner = '\u2570';
                Terminal.Gui.Application.Driver.LRCorner = '\u256F';
                Terminal.Gui.Application.Top.Add(_installManager.CreateDefaultScreen());
                Terminal.Gui.Application.Run((termEx) => {

                    return true;
                });
            } catch {

                //todo: handle exception
            }

            Terminal.Gui.Application.Shutdown();


            //Environment.ProcessorCount
            //var table = new Table();
            //table.Border(TableBorder.Rounded);
            //table.AddColumn("Property");
            //table.AddColumn("Value");
            //table.AddRow(new Markup(nameof(RuntimeInformation.ProcessArchitecture)), new Panel($"[gray]{RuntimeInformation.ProcessArchitecture}[/]"));
            //table.AddRow(new Markup(nameof(RuntimeInformation.OSArchitecture)), new Panel($"[gray]{RuntimeInformation.OSArchitecture}[/]"));
            //table.AddRow(new Markup(nameof(RuntimeInformation.FrameworkDescription)), new Panel($"[gray]{RuntimeInformation.FrameworkDescription}[/]"));
            //table.AddRow(new Markup(nameof(RuntimeInformation.OSDescription)), new Panel($"[gray]{RuntimeInformation.OSDescription}[/]"));
            //table.AddRow(new Markup(nameof(RuntimeInformation.RuntimeIdentifier)), new Panel($"[gray]{RuntimeInformation.RuntimeIdentifier}[/]"));
            //table.AddRow(new Markup("Current Location"), new Panel($"[gray]{AppContext.BaseDirectory}[/]"));

            //var topRightGrid = new Grid();
            //topRightGrid.AddColumn();
            //topRightGrid.AddRow(Align.Center(
            //        new Markup("[blue]System Info[/]"),
            //        VerticalAlignment.Top));
            //topRightGrid.AddRow(table);

            //var topRightPanel = new Panel(topRightGrid).Expand();



            //layout["right"]["top"].Update(topRightPanel);
            ////layout["left"].Update(new Panel(
            ////    AnsiConsole.Prompt(new SelectionPrompt<string>()
            ////    .Title("What's your [green]favorite fruit[/]?")
            ////    .PageSize(10)
            ////    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
            ////    .AddChoices(new[] {
            ////        "Apple", "Apricot", "Avocado",
            ////        "Banana", "Blackcurrant", "Blueberry",
            ////        "Cherry", "Cloudberry", "Cocunut",
            ////    }))).Expand());
            //AnsiConsole.Live(layout)
            //.AutoClear(true)   // Do not remove when done
            //.Overflow(VerticalOverflow.Ellipsis) // Show ellipsis when overflowing
            //.Cropping(VerticalOverflowCropping.Top) // Crop overflow at top
            //.Start(ctx => {
            //    // Omitted
            //});
            ////Console.WriteLine($"{nameof(RuntimeInformation.ProcessArchitecture)}: {RuntimeInformation.ProcessArchitecture}");
            ////Console.WriteLine($"{nameof(RuntimeInformation.OSArchitecture)}: {RuntimeInformation.OSArchitecture}");
            ////Console.WriteLine($"{nameof(RuntimeInformation.FrameworkDescription)}: {RuntimeInformation.FrameworkDescription}");
            ////Console.WriteLine($"{nameof(RuntimeInformation.OSDescription)}: {RuntimeInformation.OSDescription}");
            ////Console.WriteLine($"{nameof(RuntimeInformation.RuntimeIdentifier)}: {RuntimeInformation.RuntimeIdentifier}");
            ////Console.WriteLine($"Our location is: {AppContext.BaseDirectory}");



            // Echo the fruit back to the terminal
            // AnsiConsole.WriteLine($"I agree. {fruit} is tasty!");


            // const string SHELL_COMMAND_CAT = "cat";
            // const string COMMAND_SHELL = "/bin/sh";
            // const string SHELL_ARG_INTERPRET = "-c";
            // const string SHELL_COMMAND_LS_ARG_SYS_INFO = "ls /etc/*-release";

            ////var files = new DirectoryInfo(AppContext.BaseDirectory).GetFiles();
            ////foreach (var file in files) {
            ////    Console.WriteLine(file);
            ////}
            //if(RuntimeInformation.OSArchitecture == Architecture.Wasm) {
            //    Console.WriteLine("You are using WASI, let's see if we can discover your OS...");
            //    //await using var pipeClient = new NamedPipeClientStream(".", "/thepipe", PipeDirection.In);
            //    //await using var pipeServer = new NamedPipeServerStream("/thepipe", PipeDirection.Out);
            //    //await pipeServer.WaitForConnectionAsync();
            //    //pipeClient.Connect();
            //    //await using var sw = new StreamWriter(pipeServer);
            //    //sw.AutoFlush = true;
            //    //sw.WriteLine("uname -s");

            //    //File.WriteAllText("/thepipe", "end");
            //    var task = Task.Run(() => {
            //        using (StreamReader readtext = new StreamReader("/thepiperesponse")) {
            //            string? readText;
            //            while ((readText = readtext.ReadLine()) != null) {
            //                Console.WriteLine($"response: {readText}");
            //            }
            //        }
            //    });

            //    using (StreamWriter writetext = new StreamWriter("/thepipe")) {
            //        writetext.WriteLine("uname -s");
            //    }




            //    //using StreamWriter file = new("/thepipe", append: false);
            //    //file.AutoFlush = true;
            //    //file.WriteLine("uname -sda");
            //    //File.WriteAllText("/thepipe", "uname -sa");

            //    //await DiscoverOs();
            //    //using var sr = new StreamReader(pipeClient);
            //    //string? temp;
            //    //while ((temp = sr.ReadLine()) != null) {
            //    //    Console.WriteLine("Received from server: {0}", temp);
            //    //}
            //}



            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            //    Console.WriteLine("GREAT, you are using Linux! Let's figure out which distro...");
            //    await DiscoverDistro();
            //}
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
            //    Console.WriteLine("GREAT, you are using FreeBSD! Continuing...");
            //    await StartInstall();
            //}
            //else {
            //    Console.Error.WriteLine("Make sure you are running this on FreeBSD or Linux, otherwise the install will not work. Hint: You are not running on FreeBSD or Linux :(");
            //}


            //async Task StartInstall() {
            //    var hostname = string.Empty;
            //    var hostnameFound = (string result) => {
            //        hostname = result;
            //        Console.WriteLine($"Hostname found: {hostname}");
            //    };
            //    var hostnameNotFound = (string result) => {
            //        Console.Error.WriteLine($"No hostname found: {result}");

            //    };
            //    var cmd = Cli.Wrap(COMMAND_UNAME).WithArguments(UNAME_ARG_NAME) | (hostnameFound, hostnameNotFound);

            //    try {
            //        await cmd.ExecuteAsync();
            //    }
            //    catch (Exception ex) {
            //        hostnameNotFound(ex.Message);
            //    }


            //}

            // async Task<string> getOsReleaseFile(bool debug = false) {
            //     var response = string.Empty;
            //     var sysInfo = (string result) => {
            //         response = result ?? string.Empty;
            //         if (debug) {
            //             Console.WriteLine($"Release file found: {result}");
            //         }
            //     };

            //     var sysInfoFailed = (string result) => {
            //         Console.WriteLine($"Locating release file failed ({result})");
            //     };

            //     var syscmd =
            //         Cli.Wrap(COMMAND_SHELL)
            //             .WithArguments(new[] { SHELL_ARG_INTERPRET, SHELL_COMMAND_LS_ARG_SYS_INFO })
            //         | (sysInfo, sysInfoFailed);

            //     try {
            //         await syscmd.ExecuteAsync();
            //         return response;
            //     } catch (Exception ex) {
            //         sysInfoFailed(ex.Message);
            //     }

            //     return string.Empty;
            // }

            // async Task<SystemInfoModel> getOsInfo(string releaseFile, bool debug = false) {
            //     var model = new SystemInfoModel();
            //     var response = new StringBuilder();
            //     var sysInfo = (string result) => {
            //         response.AppendLine(result ?? string.Empty);
            //         if (debug) {
            //             Console.WriteLine($"Extracted os info: {result}");
            //         }
            //     };

            //     var sysInfoFailed = (string result) => {
            //         Console.WriteLine($"Extracting os info failed ({result})");
            //     };

            //     var syscmd =
            //         Cli.Wrap(COMMAND_SHELL)
            //             .WithArguments(new[] { SHELL_ARG_INTERPRET, $"{SHELL_COMMAND_CAT} {releaseFile}" })
            //         | (sysInfo, sysInfoFailed);

            //     try {
            //         await syscmd.ExecuteAsync();
            //         string pattern = @"^(?<key>[^=]+)=""?(?<value>[^""]*)""?$";
            //         Regex regex = new Regex(pattern, RegexOptions.Multiline);
            //         string targetString = response.ToString();

            //         foreach (var match in regex.Matches(targetString).Cast<Match>()) {
            //             if (match.Success) {
            //                 var keyLookup = match.Groups["key"].Value.ToLower().Replace("_", string.Empty);
            //                 var value = match.Groups["value"].Value;
            //                 if (nameof(model.ID).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.ID = value;
            //                 } else if (nameof(model.Name).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.Name = value;
            //                 } else if (nameof(model.VersionCodeName).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.VersionCodeName = value;
            //                 } else if (nameof(model.SupportUrl).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.SupportUrl = new Uri(value);
            //                 } else if (nameof(model.BugReportUrl).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.BugReportUrl = new Uri(value);
            //                 } else if (nameof(model.HomeUrl).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.HomeUrl = new Uri(value);
            //                 } else if (nameof(model.PrettyName).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.PrettyName = value;
            //                 } else if (nameof(model.VersionId).ToLower().CompareTo(keyLookup) == 0) {
            //                     model.VersionId = value;
            //                 }
            //             }
            //         }

            //         return model;
            //     } catch (Exception ex) {
            //         sysInfoFailed(ex.Message);
            //     }

            //     return model;
            // }

            // async Task DiscoverDistro(bool debug = false) {
            //     var sysInfoFailed = (string result) =>
            //         Console.WriteLine($"Details system info not located using method #1 ({result})");

            //     try {
            //         var releaseFile = await getOsReleaseFile();
            //         if (string.IsNullOrWhiteSpace(releaseFile)) {
            //             sysInfoFailed("Release file is not found");
            //             return;
            //         }

            //         var systemInfo = await getOsInfo(releaseFile);
            //         Console.WriteLine($"Got it: {systemInfo.PrettyName} 😉");
            //     } catch (Exception ex) {
            //         sysInfoFailed(ex.Message);
            //     }
            // }
        } catch (Exception ex) {
            Console.Error.WriteLine($"General failure: {ex.Message} :: {ex}");
        } finally {
            Terminal.Gui.Application.Shutdown();
        }
    }
}