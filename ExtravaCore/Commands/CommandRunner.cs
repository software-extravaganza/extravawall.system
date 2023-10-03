using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using ExtravaCore.Commands;
using ExtravaCore.Commands.Framework;

namespace ExtravaCore.Commands;

public class CommandRunner : ICommandRunner {
    private CommandServiceProvider _commandProvider;

    public CommandRunner(CommandServiceProvider commandProvider) {
        _commandProvider = commandProvider;
    }

    // public TCommand For<TCommand>() where TCommand : ICommandWrapperWithNoInputResult<TCommand> {

    // }

    // public TCommand For<TCommand, TOptions>(Action<TOptions> setOptions)
    //     where TCommand : ICommandWrapperWithOptions<TCommand, TOptions>
    //     where TOptions : new() {
    //     var command = _commandProvider.GetService<TCommand>();
    //     command.OS = _commandProvider.GetService<OperatingSystem>();
    //     return command;
    // }

    // public ForCommandNoInputProperty<TCommand> For<TCommand>() where TCommand : ICommandWrapperWithNoInputResult<TCommand>, new() {
    //     return new ForCommandNoInputProperty<TCommand>();
    // }

    // public ForCommandWithOptionsProperty<TCommand, TOptions> For<TCommand, TOptions>(Action<TOptions> setOptions = null)
    //     where TCommand : ICommandWrapperWithOptions<TCommand, TOptions>, new()
    //     where TOptions : new() {
    //     return new ForCommandWithOptionsProperty<TCommand, TOptions>(setOptions);
    // }

    public IForCommandWithNoInput<TCommand> For<TCommand>() where TCommand : ICommandWrapperWithNoInputResult<TCommand>, new() {
        return new ForCommandWithNoInput<TCommand>(_commandProvider);
    }

    public IForCommandWithOptions<TCommand> For2<TCommand>() where TCommand : ICommand {
        return new ForCommandWithOptions<TCommand>(_commandProvider);
    }

    private class ForCommandWithNoInput<TCommand> : IForCommandWithNoInput<TCommand> where TCommand : ICommandWrapperWithNoInputResult<TCommand>, new() {

        private CommandServiceProvider _commandProvider;

        public ForCommandWithNoInput(CommandServiceProvider commandProvider) {
            _commandProvider = commandProvider;
        }

        public TCommand WithNoInput {
            get {
                var command = _commandProvider.GetService<TCommand>();
                command.OS = _commandProvider.GetService<OperatingSystem>();
                return command;
            }
        }
    }

    private class ForCommandWithOptions<TCommand> : IForCommandWithOptions<TCommand> where TCommand : ICommandWithOptions {
        private CommandServiceProvider _commandProvider;

        public ForCommandWithOptions(CommandServiceProvider commandProvider) {
            _commandProvider = commandProvider;
        }

        public TCommand WithOptions(Action<TOptions> setOptions) {
            var command = _commandProvider.GetService<TCommand>();
            command.OS = _commandProvider.GetService<OperatingSystem>();
            return command;
        }
    }
}

