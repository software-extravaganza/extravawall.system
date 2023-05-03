using Jab;
namespace ExtravaCore;

[ServiceProvider]
[Singleton<ExtravaServiceProviderCore>(Factory = nameof(ExtravaServiceProviderCoreFactory))]
[Scoped<TaskCompletionSource>]
[Scoped<IElevator, Elevator>]
[Scoped<IProcessManager, ProcessManager>]
public partial class ExtravaServiceProviderCore {
    public ExtravaServiceProviderCore ExtravaServiceProviderCoreFactory() => this;
}
