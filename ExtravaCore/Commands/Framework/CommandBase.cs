using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;

namespace ExtravaCore.Commands.Framework;


public abstract partial class CommandBase : ICommand {
    private ICommandDriver? _driver;
    private OperatingSystem? _os;
    public OperatingSystem OS {
        get { return _os ?? throw new InvalidOperationException("Operating System not set."); }
        set {
            _driver = value.CommandDriverFactory();
            _os = value;
        }
    }

    public ICommandDriver Driver {
        get { return _driver ?? throw new InvalidOperationException("Operating System not set."); }
    }

    public void SetCommandView(ICommandView view) {
        Driver.SetCommandView(view);
    }

    public void SetOutput(CommandOutputType? overriddenOutputType) {
        Driver.SetOutput(overriddenOutputType);
    }
}