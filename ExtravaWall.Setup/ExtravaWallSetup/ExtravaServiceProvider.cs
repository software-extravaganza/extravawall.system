using Jab;
using ExtravaCore;
using ExtravaCore.Commands;

namespace ExtravaWallSetup;

[ServiceProvider]
[Import<IExtravaServiceProviderCoreModule>]
[Singleton<ExtravaServiceProvider>(Factory = nameof(ExtravaServiceProviderFactory))]
[Singleton<InstallBooter>]
[Singleton<Program>]
[Singleton<InstallManager>]
[Singleton<Stages.Framework.StageManager>]
[Singleton<GUI.DefaultScreen>]
[Transient<TaskCompletionSource>]
[Singleton<GUI.Framework.VirtualConsoleManager>]
[Singleton<Stages.Framework.EmptyStep>]
[Singleton<Stages.EndStage>]
[Singleton<Stages.Initialization.SystemInfoStep>]
[Singleton<Stages.Initialization.MenuStageStep>]
[Singleton<Stages.Install.InstallBeginStep>]
[Singleton<Stages.InstallCheckSystem.InstallCheckSystemStep>]
[Transient<IElevator, Elevator>]
[Transient<IProcessManager, ProcessManager>]
public partial class ExtravaServiceProvider : IExtravaServiceProviderCoreModule {
    public ExtravaServiceProvider ExtravaServiceProviderFactory() => this;


}
