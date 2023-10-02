using System.Reflection.PortableExecutable;
using ExtravaCore.Commands.Framework;
using Jab;
namespace ExtravaCore.Commands;

[ServiceProvider]
[Singleton<CommandServiceProvider>(Factory = nameof(CommandServiceProviderFactory))]
[Singleton<OperatingSystem>]
[Singleton<CommandSettings>]
//Command Drivers
[Transient<LinuxCommandDriver>]
//Commands
[Transient<CommandMachineName>]
[Transient<CommandMachineOs>]
[Transient<CommandMachineArchitecture>]
[Transient<CommandMachineAll>]
[Transient<CommandPackagesInstalled>]
public partial class CommandServiceProvider {
    public CommandServiceProvider CommandServiceProviderFactory() => this;

}