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
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.IO.XmlTasks;
using static Nuke.Common.EnvironmentInfo;

class Build : NukeBuild {
    public static int Main() => Execute<Build>(x => x.Test);
    [Parameter] readonly string Configuration = "Release";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        //.OnlyWhenDynamic(() => HasArgument("rebuild"))
        .Executes(() => {
            foreach (var dir in (RootDirectory / "./ExtravaWall").GlobDirectories("**/bin/{Configuration}")) {
                EnsureCleanDirectory(dir);
            }
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() => {
            DotNetBuild(s => s.SetProjectFile(RootDirectory / "ExtravaWall.sln").
            SetConfiguration(Configuration));
        });


    [Parameter("Collect code coverage. Default is 'true'")] readonly bool? Cover = true;
    [Parameter("Coverage threshold. Default is 80%")] readonly int Threshold = 80;

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() => {
            DotNetTest(s => s.SetProjectFile(RootDirectory / "ExtravaWall.sln")
            .EnableNoBuild()
            .EnableNoRestore()
            .SetDataCollector("XPlat Code Coverage")
            .SetConfiguration(Configuration)
            .SetLoggers("trx;LogFileName=TestResults.trx")
            .SetVerbosity(DotNetVerbosity.Normal)
            .SetProcessArgumentConfigurator(arguments => arguments.Add("/p:CollectCoverage={0}", Cover)
                .Add("/p:CoverletOutput={0}/", ArtifactsDirectory / "coverage")
                //.Add("/p:Threshold={0}", Threshold)
                .Add("/p:UseSourceLink={0}", "true")
                .Add("/p:CoverletOutputFormat={0}", "cobertura%2clcov%2copencover"))
            .SetResultsDirectory(ArtifactsDirectory / "tests"));
            //DotNetTest(RootDirectory / "ExtravaWall.sln", new DotNetTestSettings{Configuration = Configuration, NoBuild = true, });
        });
}