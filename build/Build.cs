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

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    // [Nuke.Common.Tools.ReportGenerator.ReportGeneratorSettings(]
    // [NuGetPackage(
    // packageId: "dotnet-reportgenerator-globaltool",
    // packageExecutable: "ReportGenerator.dll",
    // // Must be set for tools shipping multiple versions
    // Framework = "net7.0")]
    // readonly Tool ReportGenerator;

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
                    DotNetTest(
                        s =>
                            s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
                                .EnableNoBuild()
                                .EnableNoRestore()
                                .SetConfiguration(Configuration)
                                .SetLoggers("\"trx;LogFileName=tests/TestResults.trx\"")
                                .SetVerbosity(DotNetVerbosity.Normal)
                                .SetProcessArgumentConfigurator(
                                    arguments =>
                                        arguments
                                            .Add("/p:AltCover={0}", "true")
                                            .Add("/p:AltCoverForce={0}", "true")
                                            .Add("/p:AltCoverShowSummary={0}", "true")
                                            .Add("/p:AltCoverVerbosity={0}", "Verbose") //Verbose, Info (default), Warning, Error, or Off
                                            .Add("/p:AltCoverAssemblyExcludeFilter={0}", "Test")
                                            .Add(
                                                "/p:AltCoverTypeFilter={0}",
                                                "Test|Jab|Microsoft.CodeAnalysis|System.Runtime"
                                            )
                                            .Add("/p:AltCoverFileFilter={0}", ".*(?!Jab).*")
                                            .Add("/p:AltCoverAssemblyFilter={0}", "?Extrava")
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

                    var testResultsFile = (ArtifactsDirectory / "tests/TestResults.trx").ToString();
                    // string xml = File.ReadAllText(testResultsFile);
                    // XmlDocument doc = new XmlDocument();
                    // doc.LoadXml(xml);
                    // string json = JsonConvert.SerializeXmlNode(doc);
                    // File.WriteAllText(testResultsFile.Replace(".trx", ".json"), json);
                    var converter = new TestResultConverter();
                    var root = converter.ConvertTrxToJson(testResultsFile);
                    var json = converter.SerializeToJson(root);
                    File.WriteAllText(testResultsFile.Replace(".trx", ".json"), json);

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
                        // ReportGenerator(s => s
                        //     .SetFramework("net7.0")
                        //     .SetReports(RootDirectory.GlobFiles("**/*.Test/artifacts/coverage/coverage.json").Select(x => x.ToString()).ToArray())
                        //     .SetReportTypes(ReportTypes.JsonSummary)
                        //     .SetTargetDirectory(ArtifactsDirectory / "coverage")
                        // );
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
                    //DotNetTest(RootDirectory / "ExtravaWall.sln", new DotNetTestSettings{Configuration = Configuration, NoBuild = true, });
                });

    Target Docs =>
        _ =>
            _.DependsOn(Test)
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

                    foreach (
                        var dir in RootDirectory
                            .GlobFiles("**/*.Test.csproj")
                            .Select(x => x.Parent)
                            .Distinct()
                    )
                    {
                        var testProjArtifactsDir = dir / "artifacts";
                        var featureFolder = dir / "Features";
                        var livingDocGenerate = await Cli.Wrap("livingdoc")
                            .WithArguments(
                                new string[]
                                {
                                    "feature-folder",
                                    featureFolder,
                                    "--output",
                                    testProjArtifactsDir / "tests/index.html",
                                    "-t",
                                    ArtifactsDirectory / "tests/TestResults.json"
                                }
                            )
                            .WithValidation(CommandResultValidation.None)
                            .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
                            .ExecuteAsync();

                        Console.WriteLine(
                            $"Living doc status for {featureFolder}: {(livingDocGenerate.ExitCode == 0 ? "success" : "fail")}"
                        );
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                });
}

public class TestResultConverter
{
    public class ExecutionResult
    {
        public string ContextType { get; set; }
        public string FeatureFolderPath { get; set; }
        public string FeatureTitle { get; set; }
        public string ScenarioTitle { get; set; }
        public List<string> ScenarioArguments { get; set; }
        public string Status { get; set; }
        public List<StepResult> StepResults { get; set; }
        public object Outputs { get; set; }
    }

    public class StepResult
    {
        public string Duration { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public string Outputs { get; set; }
    }

    public class Root
    {
        public List<object> Nodes { get; set; }
        public DateTime ExecutionTime { get; set; }
        public DateTime GenerationTime { get; set; }
        public Guid PluginUserSpecFlowId { get; set; }
        public object CLIUserSpecFlowId { get; set; }
        public List<ExecutionResult> ExecutionResults { get; set; }
        public object StepReports { get; set; }
    }

    public Root ConvertTrxToJson(string trxFilePath)
    {
        // Load the XML file
        var xdoc = XDocument.Load(trxFilePath);

        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        // Extract data from XML and map it to your classes
        var root = new Root
        {
            Nodes = new List<object>(), // This is dependent on your TRX structure
            ExecutionTime = DateTime.Parse(
                xdoc.Descendants(ns + "Times").First().Attribute("start").Value
            ),
            GenerationTime = DateTime.Parse(
                xdoc.Descendants(ns + "Times").First().Attribute("finish").Value
            ),
            PluginUserSpecFlowId = Guid.NewGuid(), // This value is specific to your usage, replace with appropriate mapping
            CLIUserSpecFlowId = null, // This value is specific to your usage, replace with appropriate mapping
            ExecutionResults = new List<ExecutionResult>(),
            StepReports = null // This value is specific to your usage, replace with appropriate mapping
        };

        // Map the ExecutionResults
        var unitTestElements = xdoc.Descendants(ns + "UnitTestResult");
        foreach (var element in unitTestElements)
        {
            var executionResult = new ExecutionResult
            {
                ContextType = "Scenario", // Assuming all results are from Unit Tests
                FeatureFolderPath = "Features", // This needs to be replaced with correct mapping
                FeatureTitle = "N/A", // This needs to be replaced with correct mapping
                ScenarioTitle = element.Attribute("testName").Value,
                ScenarioArguments = new List<string>(), // This needs to be replaced with correct mapping
                Status = element.Attribute("outcome").Value,
                StepResults = new List<StepResult>
                {
                    new StepResult
                    {
                        Duration = "",
                        Status = element.Attribute("outcome").Value,
                        Error = "",
                        Outputs = ""
                    }
                }, // This needs to be replaced with correct mapping
                Outputs = null // This needs to be replaced with correct mapping
            };
            root.ExecutionResults.Add(executionResult);
        }

        // Similarly map other properties...

        return root;
    }

    public string SerializeToJson(Root root)
    {
        // Serialize to JSON
        var jsonString = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
        return jsonString;
    }
}
