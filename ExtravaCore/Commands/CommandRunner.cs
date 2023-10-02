using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;

namespace ExtravaCore.Commands;
public interface ICommandRunner {
    TCommand For<TCommand>() where TCommand : ICommand;
}

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
