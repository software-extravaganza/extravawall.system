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

class Build : NukeBuild {
    public static int Main() => Execute<Build>(x => x.Test);
    // [Nuke.Common.Tools.ReportGenerator.ReportGeneratorSettings(]
    // [NuGetPackage(
    // packageId: "dotnet-reportgenerator-globaltool",
    // packageExecutable: "ReportGenerator.dll",
    // // Must be set for tools shipping multiple versions
    // Framework = "net7.0")]
    // readonly Tool ReportGenerator;

    [Parameter] readonly string Configuration = "Debug";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        //.OnlyWhenDynamic(() => HasArgument("rebuild"))
        .Executes(() => {
            foreach (var dir in (RootDirectory / "./ExtravaWall").GlobDirectories("**/bin/{Configuration}")) {
                EnsureCleanDirectory(dir);
            }

            foreach (var dir in RootDirectory.GlobFiles("**/*.Test.csproj").Select(x => x.Parent).Distinct()) {
                var testProjArtifactsDir = dir / "artifacts";
                var testProjCoverageDir = testProjArtifactsDir / "coverage";
                EnsureCleanDirectory(testProjArtifactsDir);
                EnsureExistingDirectory(testProjCoverageDir);
            }

            EnsureCleanDirectory(ArtifactsDirectory / "coverage");
            EnsureCleanDirectory(ArtifactsDirectory / "tests");
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() => {
            DotNetBuild(s => s.SetProjectFile(RootDirectory / "ExtravaWall.sln").
            SetConfiguration(Configuration));
        });


    [Parameter("Collect code coverage. Default is 'true'")] readonly bool? Cover = true;
    [Parameter("Coverage threshold. Default is 80%")] readonly int Threshold = 0;

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() => {
            DotNetTest(s => s
                .SetProjectFile(RootDirectory / "ExtravaWall.sln")
                .EnableNoBuild()
                .EnableNoRestore()
                .SetConfiguration(Configuration)
                .SetLoggers("\"trx;LogFileName=TestResults.trx\"")
                .SetVerbosity(DotNetVerbosity.Normal)
                .SetProcessArgumentConfigurator(arguments => arguments
                    .Add("/p:AltCover={0}", Cover)
                    .Add("/p:AltCoverForce={0}", "true")
                    .Add("/p:AltCoverShowSummary={0}", "true")
                    .Add("/p:AltCoverVerbosity={0}", "Warning") //Verbose, Info (default), Warning, Error, or Off
                    .Add("/p:AltCoverAssemblyExcludeFilter={0}", "Test")
                    .Add("/p:AltCoverTypeFilter={0}", "Test|Jab|Microsoft.CodeAnalysis|System.Runtime")
                    .Add("/p:AltCoverAssemblyFilter={0}", "?Extrava")
                    .Add("/p:AltCoverAttributeFilter={0}", "ExcludeFromCodeCoverage")
                    .Add("/p:AltCoverReport={0}", "artifacts/coverage/coverage.xml")
                    .Add("/p:AltCoverCobertura={0}", "artifacts/coverage/Cobertura.xml")
                    .Add("/p:AltCoverLcovReport={0}", "artifacts/coverage/lcov.info")
                    .Add("/p:AltCoverOpenCover={0}", "artifacts/coverage/coverage.opencover.xml")
                    .Add("/p:AltCoverHtml={0}", "artifacts/coverage/html/coverage.html")
                    .Add("/p:AltCoverThreshold={0}", Threshold)
                    .Add("/p:AltCoverThresholdType={0}", "line")
                    .Add("/p:CopyLocalLockFileAssemblies={0}", "true")
                )
                .SetResultsDirectory(ArtifactsDirectory / "tests")
            );

            if (Cover is true) {
                ReportGenerator(s => s
                    .SetFramework("net7.0")
                    .SetReports(RootDirectory.GlobFiles("**/*.Test/artifacts/coverage/Cobertura.xml").Select(x => x.ToString()).ToArray())
                    .SetReportTypes(ReportTypes.Cobertura)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage")
                );
                ReportGenerator(s => s
                    .SetFramework("net7.0")
                    .SetReports(RootDirectory.GlobFiles("**/*.Test/artifacts/coverage/lcov.info").Select(x => x.ToString()).ToArray())
                    .SetReportTypes(ReportTypes.lcov)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage")
                );
                ReportGenerator(s => s
                    .SetFramework("net7.0")
                    .SetReports(RootDirectory.GlobFiles("**/*.Test/artifacts/coverage/coverage.xml").Select(x => x.ToString()).ToArray())
                    .SetReportTypes(ReportTypes.Html)
                    .SetTargetDirectory(ArtifactsDirectory / "coverage/html")
                );
            }
            //DotNetTest(RootDirectory / "ExtravaWall.sln", new DotNetTestSettings{Configuration = Configuration, NoBuild = true, });
        });


}