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

    public TCommand For<TCommand>() where TCommand : ICommand {
        var command = _commandProvider.GetService<TCommand>();
        command.OS = _commandProvider.GetService<OperatingSystem>();
        return command;
    }
}
