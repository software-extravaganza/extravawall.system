using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtravaCore;

namespace ExtravaCore.Commands {



    public interface ICommand<TResult> {
        Task<TResult> ExecuteAsync();
        void SetOptions(ICommandOptions options);
    }
    
    public class CommandDescriptor<TCommand, TResult, TOptions>
    where TCommand : ICommand<TResult>, new()
    where TOptions : ICommandOptions {
        TCommand _command;
        public CommandDescriptor() {
            _command = new TCommand();
        }

        public void SetOptions(Action<TOptions> setOptions) {
            _command.setOptions(_options);
            return this;
        }
    }

    public class CommandRunner2 {
        public CommandBuilder<TCommand, TResult, TOptions> For2<TCommand, TResult, TOptions>(CommandDescriptor<TCommand, TResult, TOptions> descriptor = null)
            where TCommand : ICommand<TResult>, new()
            where TOptions : ICommandOptions, new() {
            return new CommandBuilder<TCommand, TResult, TOptions>();
        }

        public class CommandBuilder<TCommand, TResult, TOptions>
            where TCommand : ICommand<TResult>, new()
            where TOptions : ICommandOptions, new() {
            private TOptions _options = new TOptions();

            public CommandBuilder<TCommand, TResult, TOptions> Options(Action<TOptions> setOptions) {
                setOptions(_options);
                return this;
            }

            public async Task<TResult> RunAsync() {
                var command = new TCommand(); // Consider passing options here.
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
    public class CommandPackagesInstalled : ICommand<string> {
        public Task<string> ExecuteAsync() {
            return Task.FromResult("Command Executed");
        }
    }

    public class CommandPackagesInstalledOptions : ICommandOptions {
        public string Package { get; set; }
    }

    public class Bob {
        public Bob() {
            CommandDescriptors.PackagesInstalled.SetOptions(o => o.Package = "neofetch");
        }
    }
}