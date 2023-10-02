using ExtravaCore.Commands;
using Jab;
namespace ExtravaCore;

[ServiceProviderModule]
[Singleton<CommandSettings>]
[Singleton<OperatingSystem>]
[Transient<TaskCompletionSource>]
[Transient<IElevator, Elevator>]
[Transient<IProcessManager, ProcessManager>]
[Singleton<ICommandRunner, CommandRunner>]
[Singleton<CommandServiceProvider>]
public interface IExtravaServiceProviderCoreModule { }