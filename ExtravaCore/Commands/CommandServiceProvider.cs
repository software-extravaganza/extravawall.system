using System.Reflection.PortableExecutable;
using Jab;
namespace ExtravaCore.Commands.Framework;

[ServiceProvider]
[Singleton<CommandServiceProvider>(Factory = nameof(CommandServiceProviderFactory))]
[Singleton<OperatingSystem>]
[Singleton<CommandSettings>]
//Command Drivers
[Transient<LinuxCommandDriver>]
//Commands
[Transient<CommandMachineName>]
[Transient<CommandMachineOs>]
public partial class CommandServiceProvider {
    public CommandServiceProvider CommandServiceProviderFactory() => this;

}