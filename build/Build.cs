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
                    //var testResultsFile = (ArtifactsDirectory / "tests/TestResults.trx").ToString();
                    // string xml = File.ReadAllText(testResultsFile);
                    // XmlDocument doc = new XmlDocument();
                    // doc.LoadXml(xml);
                    // string json = JsonConvert.SerializeXmlNode(doc);
                    // File.WriteAllText(testResultsFile.Replace(".trx", ".json"), json);





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

                    foreach (
                        var testExecutionFile in RootDirectory.GlobFiles(
                            "artifacts/tests/*.TestResults.xml"
                        )
                    )
                    {
                        var newSpecFlowFileName =
                            $"{testExecutionFile.NameWithoutExtension}.specFlow.xml";
                        var destinationSpecFlowName =
                            ArtifactsDirectory / "tests" / newSpecFlowFileName;
                        var destinantionFolder = destinationSpecFlowName.Parent;
                        AbsolutePath.Create(destinantionFolder);
                        var converter = new XUnitTestResultConverter();
                        converter.ConvertXUnitToSpecflow(
                            testExecutionFile,
                            destinationSpecFlowName
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

                        // var converter = new XUnitTestResultConverter();
                        // var root = converter.ConvertXUnitToJSON(testExecutionFile);
                        // var json = converter.SerializeToJson(root);
                        // File.WriteAllText(
                        //     testExecutionFile.ToString().Replace(".xml", ".json"),
                        //     json
                        // );
                    }
                    // foreach (
                    //     var testExecutionFile in RootDirectory.GlobFiles(
                    //         "artifacts/tests/*.TestResults.xml"
                    //     )
                    // )
                    // {
                    //     var destinationDir =
                    //         testExecutionFile
                    //             .FindParent(path => path.Name.CompareTo("bin") == 0)
                    //             .Parent + "/artifacts/tests";
                    //     AbsolutePath.Create(destinationDir);
                    //     CopyFileToDirectory(
                    //         testResultsFile,
                    //         destinationDir,
                    //         FileExistsPolicy.Overwrite
                    //     );
                    // }

                    //DotNetTest(RootDirectory / "ExtravaWall.sln", new DotNetTestSettings{Configuration = Configuration, NoBuild = true, });

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
                    var livingTestResultsFile = (
                        ArtifactsDirectory / "tests/TestResults.json"
                    ).ToString();
                    var pickleTestResultsFile = (
                        ArtifactsDirectory / "tests/TestResults.xml"
                    ).ToString();
                    // var globalFeatureFolder = ArtifactsDirectory / "Features";
                    // var allTestResultFiles = RootDirectory.GlobFiles("artifacts/tests/*.TestResults.xml");
                    // foreach(var testResultFile in allTestResultFiles){
                    //     var featureRoot = testResultFile / testResultFile.Parent.Name;
                    //     var fileContent = File.ReadAllText(testResultFile);
                    //     fileContent = fileContent.Replace("`"FeatureFolderPath`":`"", "new value");
                    //     File.WriteAllText(testResultFile, fileContent);
                    // }



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

                    // foreach (
                    //     var dir in RootDirectory
                    //         .GlobFiles("**/*.Test.csproj")
                    //         .Select(x => x.Parent)
                    //         .Distinct()
                    // )
                    // {
                    // var testProjArtifactsDir = dir / "artifacts";
                    // var featureFolder = dir / "Features";
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

                    // AbsolutePath.Create(globalFeatureFolder);
                    // featureFolder
                    //     .GetFiles()
                    //     .ForEach(file =>
                    //     {
                    //         CopyFileToDirectory(
                    //             file,
                    //             globalFeatureFolder,
                    //             FileExistsPolicy.Overwrite
                    //         );
                    //     });
                    // }

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
        var fileInfo = new FileInfo(trxFilePath);
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
                FeatureFolderPath = $"{fileInfo.DirectoryName}/Features", // This needs to be replaced with correct mapping
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

public class XUnitTestResultConverter
{
    [XmlRoot(ElementName = "assemblies")]
    public class Assemblies
    {
        [XmlElement(ElementName = "assembly")]
        public List<Assembly> Assembly { get; set; }
    }

    public class Assembly
    {
        [XmlElement(ElementName = "collection")]
        public List<Collection> Collections { get; set; }
    }

    public class Collection
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "test")]
        public List<Test> Tests { get; set; }
    }

    public class Test
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "result")]
        public string Result { get; set; }
    }

    [XmlRoot("features")]
    public class Features
    {
        [XmlElement("feature")]
        public List<Feature> FeatureList { get; set; }
    }

    public class Feature
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("scenarios")]
        public Scenarios Scenarios { get; set; }
    }

    public class Scenarios
    {
        [XmlElement("scenario")]
        public List<Scenario> ScenarioList { get; set; }
    }

    public class Scenario
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("result")]
        public string Result { get; set; }
    }

    // ... Other classes remain the same ...

    public TestResultConverter.Root ConvertXUnitToJSON(string xunitFilePath)
    {
        var fileInfo = new FileInfo(xunitFilePath);
        var xdoc = XDocument.Load(xunitFilePath);

        // Assuming xUnit2 doesn’t have a namespace. If it does, replace it accordingly
        XNamespace ns = xdoc.Root.GetDefaultNamespace();

        // Adapt to the xUnit2 structure
        var root = new TestResultConverter.Root
        {
            Nodes = new List<object>(),
            ExecutionTime = DateTime.Now, // xUnit may not have ExecutionTime, replace accordingly
            GenerationTime = DateTime.Now, // xUnit may not have GenerationTime, replace accordingly
            PluginUserSpecFlowId = Guid.NewGuid(),
            CLIUserSpecFlowId = null,
            ExecutionResults = new List<TestResultConverter.ExecutionResult>(),
            StepReports = null
        };

        string getStatus(string status)
        {
            return status.Trim().ToLower() switch
            {
                "Pass" => "Passed",
                "Fail" => "Failed",
                "Skip" => "Skipped",
                _ => "Unknown"
            };
        }

        // Map the ExecutionResults
        var testElements = xdoc.Descendants(ns + "test"); // Adapt to actual xUnit2 tag
        foreach (var element in testElements)
        {
            var traits = element.Elements("traits")?.Elements("trait");
            var executionResult = new TestResultConverter.ExecutionResult
            {
                ContextType = "Scenario",
                FeatureFolderPath = $"{fileInfo.DirectoryName}/Features",
                FeatureTitle =
                    traits
                        .Where(t => t.Attribute("name")?.Value == "FeatureTitle")
                        .FirstOrDefault()
                        ?.Attribute("value")
                        ?.Value ?? "N/A", // Replace with xUnit2 attribute name
                ScenarioTitle = element.Attribute("name")?.Value, // Replace with xUnit2 attribute name
                ScenarioArguments = new List<string>(),
                Status = getStatus(element.Attribute("result")?.Value), // Replace with xUnit2 attribute name
                StepResults = new List<TestResultConverter.StepResult>
                {
                    new TestResultConverter.StepResult
                    {
                        Duration = element.Attribute("time")?.Value, // Replace with xUnit2 attribute name
                        Status = getStatus(element.Attribute("result")?.Value), // Replace with xUnit2 attribute name
                        Error = "",
                        Outputs = ""
                    }
                },
                Outputs = null
            };
            root.ExecutionResults.Add(executionResult);
        }

        // Similarly map other properties...

        return root;
    }

    public void ConvertXUnitToSpecflow(string xunitFilePath, string outputFilePath)
    {
        string convertResult(string result)
        {
            return result.Trim().ToLower() switch
            {
                "pass" => "Passed",
                "fail" => "Failed",
                "skip" => "Ignored",
                _ => "Inconclusive"
            };
        }
        // Deserialize the xUnit xml
        var serializer = new XmlSerializer(typeof(Assemblies));
        Assemblies assemblies;

        using (StreamReader reader = new StreamReader(xunitFilePath))
        {
            assemblies = (Assemblies)serializer.Deserialize(reader);
        }

        // Construct the Output String using StringBuilder
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("<!-- Pickles Begin");
        stringBuilder.AppendLine("&lt;features&gt;");

        foreach (var assembly in assemblies.Assembly)
        {
            foreach (var collection in assembly.Collections)
            {
                stringBuilder.AppendLine("\t<text>&lt;feature&gt;</text>");
                stringBuilder.AppendLine(
                    $"\t\t<text>&lt;title&gt;</text>{collection.Name}<text>&lt;/title&gt;</text>"
                );
                stringBuilder.AppendLine("\t\t<text>&lt;scenarios&gt;</text>");

                foreach (var test in collection.Tests)
                {
                    stringBuilder.AppendLine("\t\t\t<text>&lt;scenario&gt;</text>");
                    stringBuilder.AppendLine(
                        $"\t\t\t\t<text>&lt;title&gt;</text>{test.Name}<text>&lt;/title&gt;</text>"
                    );
                    stringBuilder.AppendLine(
                        $"\t\t\t\t<text>&lt;result&gt;</text>{convertResult(test.Result)}<text>&lt;/result&gt;</text>"
                    );
                    stringBuilder.AppendLine("\t\t\t<text>&lt;/scenario&gt;</text>");
                }

                stringBuilder.AppendLine("\t\t<text>&lt;/scenarios&gt;</text>");
                stringBuilder.AppendLine("\t<text>&lt;/feature&gt;</text>");
            }
        }

        stringBuilder.AppendLine("&lt;/features&gt;");
        stringBuilder.AppendLine("Pickles End -->");

        // Write the constructed string to the output file
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            writer.Write(stringBuilder.ToString());
        }

        Console.WriteLine($"Conversion Completed: {outputFilePath}");
    }

    public string SerializeToJson(TestResultConverter.Root root)
    {
        // Serialize to JSON
        var jsonString = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
        return jsonString;
    }
}
