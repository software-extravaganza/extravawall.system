using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.SpecFlow;
using Nuke.Common.Tools.SignTool;
using Nuke.Common.Utilities.Collections;
using Nuke.Common;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.SignTool;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.IO;
using Nuke.Common.IO;
using Nuke.Common;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.Logger;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.SignTool.SignToolTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using Nuke.Common.Tools.ReportGenerator;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.IO.XmlTasks;
using static Nuke.Common.EnvironmentInfo;
using CliWrap;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Text;
using NuGet.Packaging;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter]
    readonly string Configuration = "Debug";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean =>
        _ =>
            _
            //.OnlyWhenDynamic(() => HasArgument("rebuild"))
            .Executes(() =>
            {
                foreach (
                    var dir in (RootDirectory / "./ExtravaWall").GlobDirectories(
                        "**/bin/{Configuration}"
                    )
                )
                {
                    EnsureCleanDirectory(dir);
                }

                foreach (
                    var dir in RootDirectory
                        .GlobFiles("**/*.Test.csproj")
                        .Select(x => x.Parent)
                        .Distinct()
                )
                {
                    var testProjArtifactsDir = dir / "artifacts";
                    var testProjBinDir = dir / "bin";
                    var testProjCoverageDir = testProjArtifactsDir / "coverage";

                    EnsureCleanDirectory(testProjArtifactsDir);
                    EnsureCleanDirectory(testProjBinDir);
                    EnsureExistingDirectory(testProjCoverageDir);
                }

                EnsureCleanDirectory(ArtifactsDirectory / "coverage");
                EnsureCleanDirectory(ArtifactsDirectory / "tests");
            });

    Target Compile =>
        _ =>
            _.DependsOn(Clean)
                .Executes(() =>
                {
                    DotNetBuild(
                        s =>
                            s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
                                .SetConfiguration(Configuration)
                    );
                });

    [Parameter("Collect code coverage. Default is 'true'")]
    readonly bool? Cover = true;

    [Parameter("Coverage threshold. Default is 80%")]
    readonly int Threshold = 0;

    Target Test =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                    var testResultsFile = ArtifactsDirectory / "tests/TestResults.trx";
                    AbsolutePath.Create(testResultsFile.Parent);
                    //File.Create(testResultsFile).Close();
                    Exception? testException = null;
                    try
                    {
                        DotNetTest(
                            s =>
                                s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
                                    .EnableNoBuild()
                                    .EnableNoRestore()
                                    .SetConfiguration(Configuration)
                                    .SetLoggers(
                                        new[]
                                        {
                                            $"\"xunit;LogFileName=tests/{{assembly}}.{{framework}}.TestResults.xml\"",
                                            $"\"trx;LogFileName=tests/TestResults.trx\""
                                        }
                                    )
                                    .SetVerbosity(DotNetVerbosity.Normal)
                                    .SetProcessArgumentConfigurator(
                                        arguments =>
                                            arguments
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
                    }
                    catch (Exception ex)
                    {
                        testException = ex;
                    }

                    if (Cover is true)
                    {
                        ReportGenerator(
                            s =>
                                s.SetFramework("net7.0")
                                    .SetReports(
                                        RootDirectory
                                            .GlobFiles("**/*.Test/artifacts/coverage/Cobertura.xml")
                                            .Select(x => x.ToString())
                                            .ToArray()
                                    )
                                    .SetReportTypes(ReportTypes.Cobertura)
                                    .SetTargetDirectory(ArtifactsDirectory / "coverage")
                        );
                        ReportGenerator(
                            s =>
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

                        ReportGenerator(
                            s =>
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

                    foreach (
                        var testExecutionFile in RootDirectory.GlobFiles(
                            "**/*.Test/artifacts/tests/TestResults.json"
                        )
                    )
                    {
                        var projectDirectory = testExecutionFile.FindParent(
                            path => path.Name.EndsWith(".Test")
                        );

                        var newFileName = $"{projectDirectory.Name}.TestResults.json";
                        var destinationName = ArtifactsDirectory / "tests" / newFileName;
                        var destinantionFolder = destinationName.Parent;
                        AbsolutePath.Create(destinantionFolder);
                        CopyFile(testExecutionFile, destinationName, FileExistsPolicy.Overwrite);

                        var featuresFolder = projectDirectory / "Features";
                        var featuresFolderRelative = RootDirectory.GetRelativePathTo(
                            featuresFolder
                        );
                        var fileContent = File.ReadAllText(destinationName);
                        fileContent = fileContent.Replace(
                            "\"FeatureFolderPath\":\"Features\"",
                            $"\"FeatureFolderPath\":\"{featuresFolderRelative}\""
                        );
                        File.WriteAllText(destinationName, fileContent);
                    }

                    if (testException is not null)
                    {
                        throw testException;
                    }
                });

    Target Docs =>
        _ =>
            _.AssuredAfterFailure()
                .DependsOn(Test)
                .Executes(async () =>
                {
                    var livingDocInstall = await Cli.Wrap("dotnet")
                        .WithArguments(
                            new[] { "tool", "install", "--global", "SpecFlow.Plus.LivingDoc.CLI" }
                        )
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteAsync();
                    if (livingDocInstall.ExitCode != 0 && livingDocInstall.ExitCode != 1)
                    {
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
                    if (picklesInstall.ExitCode != 0 && picklesInstall.ExitCode != 1)
                    {
                        throw new Exception(
                            $"Could not install Pickles.CommandLine global dotnet tool in {nameof(Docs)} target."
                        );
                    }
                    var livingDocArgs = new List<string>
                    {
                        "feature-folder",
                        RootDirectory,
                        "--output",
                        ArtifactsDirectory / "tests/docs/living.html",
                        "--title",
                        RootDirectory.Name
                    };

                    foreach (
                        var testExecutionFile in RootDirectory.GlobFiles(
                            "artifacts/tests/*.TestResults.json"
                        )
                    )
                    {
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
                    )
                    {
                        picklesDocArgs.Add("--link-results-file");
                        picklesDocArgs.Add(testExecutionFile);
                    }

                    var picklesGenerateCommand = Cli.Wrap("pickles").WithArguments(picklesDocArgs);
                    var picklesGenerate = await picklesGenerateCommand.ExecuteAsync();
                    Console.WriteLine(
                        $"Pickles doc status for {RootDirectory}: {(picklesGenerate.ExitCode == 0 ? "success" : "fail")}"
                    );
                    Console.WriteLine($"\tcommand: {picklesGenerateCommand}");
                    Console.WriteLine();
                    Console.WriteLine();
                });
}
