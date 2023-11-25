using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CliWrap;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.SpecFlow;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

class Build : NukeBuild {
    public static int Main(string[] args) {
        _args = args;
        if (args is not null && args.Any(a => a == "abs")) {
            _useAbsolutePaths = true;
        }

        Verbosity = Verbosity.Verbose;

        return Execute<Build>(x => x.Docs);
    }

    private static string[]? _args;
    private static bool _useAbsolutePaths;

    [Parameter]
    readonly string Configuration = "Debug";

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        //.OnlyWhenDynamic(() => HasArgument("rebuild"))
        .Executes(() => {
            foreach (var dir in (RootDirectory / "./ExtravaWall").GlobDirectories("**/bin/{Configuration}")) {
                dir.CreateOrCleanDirectory();
            }

            foreach (var dir in RootDirectory.GlobFiles("**/*.Test.csproj").Select(x => x.Parent).Distinct()) {
                var testProjArtifactsDir = dir / "artifacts";
                var testProjBinDir = dir / "bin";
                var testProjCoverageDir = testProjArtifactsDir / "coverage";

                testProjArtifactsDir.CreateOrCleanDirectory();
                testProjBinDir.CreateOrCleanDirectory();
                testProjCoverageDir.CreateOrCleanDirectory();
            }

            (ArtifactsDirectory / "coverage").CreateOrCleanDirectory();
            (ArtifactsDirectory / "tests").CreateOrCleanDirectory();
        });
    Target MyTarget => _ => _
            .Executes(async () => {
                Console.Out.WriteLine("Hello!");
            });
    Target Compile => _ => _
        .DependsOn(Clean).Executes(() => {
            Console.WriteLine("Hello!");
            DotNetBuild(s =>
            s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
            .SetVerbosity(DotNetVerbosity.Detailed)
            //.SetLoggers(new[] { "\"console;verbosity=detailed\"" })
            .SetConfiguration(Configuration));
        });

    [Parameter("Collect code coverage. Default is 'true'")]
    readonly bool? Cover = true;

    [Parameter("Coverage threshold. Default is 80%")]
    readonly int Threshold = 80;

    Target Test => _ => _
        .DependsOn(Compile).Executes(() => {
            var testResultsFile = ArtifactsDirectory / "tests/TestResults.trx";
            AbsolutePath.Create(testResultsFile.Parent);
            //File.Create(testResultsFile).Close();
            Exception? testException = null;
            try {
                var os = Environment.OSVersion.Platform switch {
                    PlatformID.Unix => "linux",
                    PlatformID.MacOSX => "osx",
                    PlatformID.Win32NT => "win",
                    _ => throw new Exception("Unknown OS")
                };

                DotNetTest(s =>
                        s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
                        .EnableNoBuild()
                        .EnableNoRestore()
                        .SetConfiguration(Configuration)
                        .SetLoggers(new[]{
                                $"\"xunit;LogFileName=tests/{{assembly}}.{{framework}}.TestResults.xml\"",
                                $"\"trx;LogFileName=tests/TestResults.trx\""
                            }
                        )
                        .SetFilter($"Category=all | Category={os}")
                        .SetProcessArgumentConfigurator(arguments => arguments
                            .Add("/p:AltCover={0}", "true")
                            .Add("/p:AltCoverForce={0}", "true")
                            .Add("/p:AltCoverShowSummary={0}", "true")
                            .Add("/p:AltCoverVerbosity={0}", "Verbose") //Verbose, Info (default), Warning, Error, or Off
                            .Add(
                                "/p:AltCoverAssemblyExcludeFilter={0}",
                                ".*CliWrap.*|.*AltCover.*|.*\\.Test.*"
                            )
                            .Add(
                                "/p:AltCoverTypeFilter={0}",
                                ".*Test.*|.*Jab.*|.*Microsoft.*|System.*|.*SpecFlow.*|.*Newtonsoft.*|.*Nuke.*|.*NuGet.*|.*CliWrap.*"
                            )
                            // .Add(
                            //     "/p:AltCoverFileFilter={0}",
                            //     ".*(?!Jab).*|.*.(?!Test).*"
                            // )
                            .Add(
                                "/p:AltCoverAssemblyFilter={0}",
                                ".*Extrava.*.(dll||exe)"
                            )
                            .Add(
                                "/p:AltCoverAttributeFilter={0}",
                                "ExcludeFromCodeCoverage"
                            )
                            .Add(
                                "/p:AltCoverReport={0}",
                                "artifacts/coverage/coverage.xml"
                            )
                            .Add(
                                "/p:AltCoverCobertura={0}",
                                "artifacts/coverage/Cobertura.xml"
                            )
                            .Add(
                                "/p:AltCoverLcovReport={0}",
                                "artifacts/coverage/lcov.info"
                            )
                            .Add(
                                "/p:AltCoverJson={0}",
                                "artifacts/coverage/coverage.json"
                            )
                            .Add(
                                "/p:AltCoverOpenCover={0}",
                                "artifacts/coverage/coverage.opencover.xml"
                            )
                            .Add(
                                "/p:AltCoverHtml={0}",
                                "artifacts/coverage/html/coverage.html"
                            )
                            .Add("/p:AltCoverThreshold={0}", Threshold)
                            .Add("/p:AltCoverThresholdType={0}", "line")
                            .Add("/p:CopyLocalLockFileAssemblies={0}", "true")
                        )
                        .SetResultsDirectory(ArtifactsDirectory)
                );
            } catch (Exception ex) {
                testException = ex;
            }

            if (Cover is true) {
                ReportGenerator(s => s.SetFramework("net7.0")
                    .SetReports(
                        RootDirectory
                            .GlobFiles("**/*.Test/artifacts/coverage/Cobertura.xml")
                            .Select(x => x.ToString())
                            .ToArray()
                    )
                    .SetReportTypes(ReportTypes.Cobertura)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage")
                );
                ReportGenerator(s =>
                    s.SetFramework("net7.0")
                    .SetReports(
                        RootDirectory
                            .GlobFiles("**/*.Test/artifacts/coverage/lcov.info")
                            .Select(x => x.ToString())
                            .ToArray()
                    )
                    .SetReportTypes(ReportTypes.lcov)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage")
                );

                ReportGenerator(s =>
                    s.SetFramework("net7.0")
                    .SetReports(
                        RootDirectory
                            .GlobFiles("**/*.Test/artifacts/coverage/coverage.xml")
                            .Select(x => x.ToString())
                            .ToArray()
                    )
                    .SetReportTypes(ReportTypes.Html)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage/html")
                );
            }

            foreach (var testExecutionFile in RootDirectory.GlobFiles("**/*.Test/artifacts/tests/TestResults.json")) {
                var projectDirectory = testExecutionFile.FindParent(
                    path => path.Name.EndsWith(".Test")
                );

                var newFileName = $"{projectDirectory.Name}.TestResults.json";
                var destinationName = ArtifactsDirectory / "tests" / newFileName;
                var destinationFolder = destinationName.Parent;
                AbsolutePath.Create(destinationFolder);
                CopyFile(testExecutionFile, destinationName, FileExistsPolicy.Overwrite);

                var featuresFolder = projectDirectory / "Features";
                var featuresFolderPath = _useAbsolutePaths
                                                ? featuresFolder.ToString()
                                                : RootDirectory.GetRelativePathTo(featuresFolder).ToString();
                var fileContent = File.ReadAllText(destinationName);
                string pattern = """
                "FeatureFolderPath"\s*:\s*"([^"]|\\")*"
                """;
                string replacement = $"""
                "FeatureFolderPath":"{featuresFolderPath}"
                """;
                string result = Regex.Replace(fileContent, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
                File.WriteAllText(destinationName, result);
            }

            if (testException is not null) {
                throw testException;
            }
        });

    Target Docs => _ => _
        .AssuredAfterFailure()
        .DependsOn(Test)
        .Executes(async () => {
            var livingDocsOutput = ArtifactsDirectory / "tests/docs/living.html";
            var coverageDocsOutput = ArtifactsDirectory / "coverage/html/index.html";
            var livingDocInstall = await Cli.Wrap("dotnet")
                .WithArguments(
                    new[] { "tool", "install", "--global", "SpecFlow.Plus.LivingDoc.CLI" }
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
            if (livingDocInstall.ExitCode != 0 && livingDocInstall.ExitCode != 1) {
                throw new Exception(
                    $"Could not install SpecFlow.Plus.LivingDoc.CLI global dotnet tool in {nameof(Docs)} target."
                );
            }

            var picklesInstall = await Cli.Wrap("dotnet")
                .WithArguments(
                    new[] { "tool", "install", "--global", "Pickles.CommandLine" }
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
            if (picklesInstall.ExitCode != 0 && picklesInstall.ExitCode != 1) {
                throw new Exception(
                    $"Could not install Pickles.CommandLine global dotnet tool in {nameof(Docs)} target."
                );
            }

            var livingDocArgs = new List<string>
            {
                "feature-folder",
                RootDirectory,
                "--output",
                livingDocsOutput,
                "--title",
                RootDirectory.Name
            };

            foreach (
                var testExecutionFile in RootDirectory.GlobFiles(
                    "artifacts/tests/*.TestResults.json"
                )
            ) {
                livingDocArgs.Add("-t");
                livingDocArgs.Add(testExecutionFile);
            }

            var livingDocGenerateCommand = Cli.Wrap("livingdoc")
                .WithArguments(livingDocArgs.ToArray())
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));

            var livingDocGenerate = await livingDocGenerateCommand.ExecuteAsync();

            Console.WriteLine(
                $"Living doc status for {RootDirectory}: {(livingDocGenerate.ExitCode == 0 ? "success" : "fail")}"
            );
            Console.WriteLine($"\tcommand: {livingDocGenerateCommand}");

            var picklesDocArgs = new List<string>
            {
                $@"--feature-directory={RootDirectory}",
                $@"--output-directory={ArtifactsDirectory}/tests/docs",
                //$@"--link-results-file={pickleTestResultsFile}",
                "--test-results-format=xunit2",
                "--documentation-format=dhtml"
            };

            foreach (
                var testExecutionFile in RootDirectory.GlobFiles(
                    "artifacts/tests/*.TestResults.xml"
                )
            ) {
                picklesDocArgs.Add("--link-results-file");
                picklesDocArgs.Add(testExecutionFile);
            }

            var picklesGenerateCommand = Cli.Wrap("pickles").WithArguments(picklesDocArgs);
            var picklesGenerate = await picklesGenerateCommand.ExecuteAsync();
            Console.WriteLine(
                $"Pickles doc status for {RootDirectory}: {(picklesGenerate.ExitCode == 0 ? "success" : "fail")}"
            );

            string TerminalURL(string caption, string url) => $"\u001B]8;;{url}\a{caption}\u001B]8;;\a";
            var docsURL = TerminalURL("Docs", $"file://{livingDocsOutput}");
            var coverageURL = TerminalURL("Coverage", $"file://{coverageDocsOutput}");

            Console.WriteLine($"\tcommand: {picklesGenerateCommand}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("\x1b[36mReports\x1B[0m");
            Console.WriteLine("\x1b[46m##########################################################################");
            Console.WriteLine($"{docsURL} generated at: file://{livingDocsOutput}");
            Console.WriteLine($"{coverageURL} generated at: file://{coverageDocsOutput}");
            Console.WriteLine("##########################################################################\x1B[0m");
            Console.WriteLine();
            Console.WriteLine();
            //string output = $"Docs generated at: {escapeSequenceStart}file://{livingDocsOutput}\aThis is a link{escapeSequenceEnd}";
            //System.IO.File.WriteAllText("output.txt", output);
        });
}
