using ExtravaCore.Commands.Framework;
namespace ExtravaCore.Commands;

public interface ICommandRunner
{
    TCommand For<TCommand>() where TCommand : ICommand;
}
