using System.Collections.ObjectModel;
using System.Text;

namespace ExtravaCore.Commands;
public interface ICommand {
    void SetCommandView(ICommandView view);
    void SetOutput(CommandOutputType? overriddenOutputType);
}
