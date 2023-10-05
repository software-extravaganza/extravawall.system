using System.Collections.ObjectModel;
using System.Text;

namespace ExtravaCore.Commands.Framework;

public interface ICommandWithOptions<out TOptions> : ICommand {
    void SetOptions(Action<TOptions> setOptions);
}

public interface ICommand {
    void SetCommandView(ICommandView view);
    void SetOutput(CommandOutputType? overriddenOutputType);

    OperatingSystem OS { get; set; }
    ICommandDriver Driver { get; }
}
