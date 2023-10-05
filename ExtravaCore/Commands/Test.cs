using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using ExtravaCore;
using ExtravaCore.Commands.Framework;

namespace ExtravaCore.Commands {



    public interface ICommand<TResult> {
        Task<TResult> ExecuteAsync();
    }

    public interface ICommand<TResult, TOptions> : ICommand<TResult> {
        void SetOptions(TOptions options);
    }

    public class CommandDescriptor<TCommand, TResult, TOptions>
        where TCommand : ICommandWrapperWithOptions<TResult, TOptions>, new()
        where TOptions : ICommandOptions, new() {
        TCommand _command;
        public CommandDescriptor() {
            _command = new TCommand();
        }

        // public void SetOptions(Action<TOptions> setOptions) {
        //     _command.setOptions(_options);
        //     return this;
        // }
    }


    public class CommandDescriptor<TCommand, TResult>
        where TCommand : ICommandWrapperWithNoInputResult<TResult>, new() {
        TCommand _command;
        public CommandDescriptor() {
            _command = new TCommand();
        }

        // public void SetOptions(Action<TOptions> setOptions) {
        //     _command.setOptions(_options);
        //     return this;
        // }
    }
    public interface ICommandBuilderWithOptions<TCommand, TResult, TOptions> : ICommandBuilder<TCommand, TResult>
            where TCommand : ICommandWrapperBase<TResult>, new()
            where TOptions : ICommandOptions, new() {
        ICommandBuilder<TCommand, TResult> Options(Action<TOptions> setOptions);
    }

    public interface ICommandBuilder<TCommand, TResult>
        where TCommand : ICommandWrapperBase<TResult>, new() {
        Task<ICommandResult<TResult>> RunAsync();
    }


    public class CommandRunner : ICommandRunner {
        private CommandServiceProvider _commandProvider;

        public CommandRunner(CommandServiceProvider commandProvider) {
            _commandProvider = commandProvider;
        }

        public ICommandBuilderWithOptions<TCommand, TResult, TOptions> For<TCommand, TResult, TOptions>(CommandDescriptor<TCommand, TResult, TOptions> descriptor)
            where TCommand : ICommandWrapperWithOptions<TResult, TOptions>, ICommandWrapperBase<TResult>, new()
            where TOptions : ICommandOptions, new() {
            return new CommandBuilder<TCommand, TResult, TOptions>(_commandProvider);
        }

        public ICommandBuilder<TCommand, TResult> For<TCommand, TResult>(CommandDescriptor<TCommand, TResult> descriptor)
            where TCommand : ICommandWrapperWithNoInputResult<TResult>, ICommandWrapperBase<TResult>, new() {
            return new CommandBuilder<TCommand, TResult>(_commandProvider);
        }

        public class CommandBuilder<TCommand, TResult, TOptions> : ICommandBuilderWithOptions<TCommand, TResult, TOptions>, ICommandBuilder<TCommand, TResult>
            where TCommand : ICommandWrapperBase<TResult>, ICommandWrapperWithOptions<TOptions>, new()
            where TOptions : ICommandOptions, new() {
            private TOptions _options = new TOptions();
            private CommandServiceProvider _commandProvider;
            private TCommand _command;

            public CommandBuilder(CommandServiceProvider commandProvider) {
                _commandProvider = commandProvider;
                _command = _commandProvider.GetService<TCommand>();
            }


            public async Task<ICommandResult<TResult>> RunAsync() {
                _command.OS = _commandProvider.GetService<OperatingSystem>();
                return await _command.ExecuteAsync();
            }

            public ICommandBuilder<TCommand, TResult> Options(Action<TOptions> setOptions) {
                _command.SetOptions(setOptions);
                return this;
            }
        }

        public class CommandBuilder<TCommand, TResult> : ICommandBuilder<TCommand, TResult>
            where TCommand : ICommandWrapperWithNoInputResult<TResult>, new() {
            private CommandServiceProvider _commandProvider;

            public CommandBuilder(CommandServiceProvider commandProvider) {
                _commandProvider = commandProvider;
            }
            public async Task<ICommandResult<TResult>> RunAsync() {
                var command = _commandProvider.GetService<TCommand>();
                command.OS = _commandProvider.GetService<OperatingSystem>();
                return await command.ExecuteAsync();
            }
        }
    }



    /////////////////////////////////////////////////
    // public static class CommandDescriptors
    // {
    //     public static CommandDescriptor<CommandPackagesInstalled, string, CommandPackagesInstalledOptions> PackagesInstalled { get; }
    //         = new CommandDescriptor<CommandPackagesInstalled, string, CommandPackagesInstalledOptions>();
    // }


    [ExtravaCommand]
    public class CommandPackagesInstalled3 : CommandWrapperWithOptions<string, CommandPackagesInstalledOptions> {
        public Task<string> ExecuteAsync() {
            return Task.FromResult("Command Executed");
        }

        public void SetOptions(CommandPackagesInstalledOptions options) {
        }

        protected override Command commandGenerator(CommandPackagesInstalledOptions result) {
            throw new NotImplementedException();
        }

        protected override string convertResult(ICommandResultRaw result) {
            throw new NotImplementedException();
        }
    }

    [ExtravaCommand]
    public class CommandPackagesInstalled2 : CommandWrapperWithNoInput<List<DateTime>> {

        protected override Command commandGenerator() {
            throw new NotImplementedException();
        }

        protected override List<DateTime> convertResult(ICommandResultRaw result) {
            throw new NotImplementedException();
        }
    }

    public class CommandPackagesInstalledOptions : ICommandOptions {
        public string Package { get; set; }
    }

    // public class Bob {
    //     public async Task Go() {
    //         var runner = new CommandRunner();
    //         var results = await runner.For(CommandDescriptors.PackagesInstalled).Options(o => o.Package = "neofetch").RunAsync();
    //     }
    // }
}