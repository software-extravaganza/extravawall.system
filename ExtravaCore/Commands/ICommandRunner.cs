using System.Diagnostics;
using ExtravaCore.Commands.Framework;
namespace ExtravaCore.Commands;

public interface IForCommandWithNoInput<out TCommand> where TCommand : ICommandWrapperWithNoInputResult<TCommand> {
    TCommand WithNoInput { get; }
}

public interface IForCommandWithOptions<out TCommand, out TOptions> where TCommand : ICommandWrapperWithOptions<TCommand, TOptions> where TOptions : new() {
    TCommand WithOptions(Action<TOptions> setOptions);
}

public interface ICommandRunner {
    // IForCommandWithNoInput<TCommand> For<TCommand>() where TCommand : ICommandWrapperWithNoInputResult<TCommand>, new();

    // TCommand For2<TCommand>() where TCommand : ICommandWrapperWithOptions<TCommand>, new();
}
