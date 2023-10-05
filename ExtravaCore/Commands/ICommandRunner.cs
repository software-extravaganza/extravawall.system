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

    ICommandBuilderWithOptions<TCommand, TResult, TOptions> For<TCommand, TResult, TOptions>(CommandDescriptor<TCommand, TResult, TOptions> descriptor)
            where TCommand : ICommandWrapperWithOptions<TResult, TOptions>, ICommandWrapperBase<TResult>, new()
            where TOptions : ICommandOptions, new();

    ICommandBuilder<TCommand, TResult> For<TCommand, TResult>(CommandDescriptor<TCommand, TResult> descriptor)
         where TCommand : ICommandWrapperWithNoInputResult<TResult>, ICommandWrapperBase<TResult>, new();

    // IForCommandWithNoInput<TCommand> For<TCommand>() where TCommand : ICommandWrapperWithNoInputResult<TCommand>, new();

    // TCommand For2<TCommand>() where TCommand : ICommandWrapperWithOptions<TCommand>, new();
}
