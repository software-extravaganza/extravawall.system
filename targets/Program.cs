using static Bullseye.Targets;
using static SimpleExec.Command;

Target(
    "build",
    () => RunAsync("dotnet", "build --configuration Release --nologo --verbosity quiet")
);
Target(
    "testit",
    DependsOn("build"),
    () => RunAsync("dotnet", "test --configuration Release --no-build --nologo --verbosity quiet")
);

//Target("default", DependsOn("test"));

await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException);
