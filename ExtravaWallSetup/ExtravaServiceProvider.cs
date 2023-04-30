using Jab;
using ExtravaCore;

namespace ExtravaWallSetup;

[ServiceProvider]
[Singleton<ExtravaServiceProvider>(Factory = nameof(ExtravaServiceProviderFactory))]
[Singleton<InstallBooter>]
[Singleton<Program>]
[Singleton<InstallManager>]
[Singleton<Stages.Framework.StageManager>]
[Singleton<GUI.DefaultScreen>]
[Scoped<TaskCompletionSource>]
[Singleton<GUI.Framework.VirtualConsoleManager>]
[Singleton<Stages.Framework.EmptyStep>]
[Singleton<Stages.EndStage>]
[Singleton<Stages.Initialization.SystemInfoStep>]
[Singleton<Stages.Initialization.MenuStageStep>]
[Singleton<Stages.Install.InstallBeginStep>]
[Singleton<Stages.InstallCheckSystem.InstallCheckSystemStep>]
[Scoped<IElevator, Elevator>]
[Scoped<IProcessManager, ProcessManager>]
public partial class ExtravaServiceProvider {
    public ExtravaServiceProvider ExtravaServiceProviderFactory() => this;
}
