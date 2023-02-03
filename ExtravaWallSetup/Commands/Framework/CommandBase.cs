using CliWrap;
using ExtravaWallSetup.GUI.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup.Commands.Framework
{
    public interface ICommand {
        void SetCommandView(ICommandView view);
        void SetOutput(CommandOutputType? overriddenOutputType);
    }
    public abstract class CommandBase<T> : ICommand where T : CommandBase<T>, new() {
        public static T Instance => _instance;

        private static T _instance = new T();
        private StringBuilder _standardOutput = new StringBuilder();
        private StringBuilder _errorOutput = new StringBuilder();
        private StringBuilder _exceptionOutput = new StringBuilder();
        private IReadOnlyDictionary<string, string?> _debianEnvironmentVariables = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?> { { "DEBIAN_FRONTEND", "noninteractive" } });
        protected ICommandView _commandView { get; set; }
        protected CommandOutputType? _overriddenOutputType { get; set; }

        private void commandStandardOutput(string output) {
            _standardOutput.AppendLine(output);
            switch(_overriddenOutputType ?? CommandSettings.OutputToVirtualConsole){
                case CommandOutputType.Console:
                    Console.WriteLine(output); break;
                case CommandOutputType.VirtualConsole:
                    _commandView.WriteStandardLine(output); break;
            };
        }

        private void commandErrorOutput(string output)
        {
            _errorOutput.AppendLine(output);
            switch (_overriddenOutputType ?? CommandSettings.OutputToVirtualConsole) {
                case CommandOutputType.Console:
                    Console.Error.WriteLine(output); break;
                case CommandOutputType.VirtualConsole:
                    _commandView.WriteErrorLine(output); break;
            };
        }

        private void commandExceptionOutput(string output)
        {
            _exceptionOutput.AppendLine(output);
            switch (_overriddenOutputType ?? CommandSettings.OutputToVirtualConsole) {
                case CommandOutputType.Console:
                    Console.Error.WriteLine(output); break;
                case CommandOutputType.VirtualConsole:
                    _commandView.WriteExceptionLine(output); break;
            };
        }

        protected async Task<CommandResultPlus> RunRawAsync(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null)
        {
            var standardOutputDelegate = (string o) => {
                commandStandardOutput(o);
                customStandardOutput?.Invoke(o);
            };

            var errorOutputDelegate = (string o) => {
                commandErrorOutput(o);
                customErrorOutput?.Invoke(o);
            };

            var finalCommand = prepareCommand(command) | (standardOutputDelegate, errorOutputDelegate);
            var startTime = DateTimeOffset.Now;
            CommandResult? commandResult;
            try
            {
                commandResult = await finalCommand.ExecuteAsync();
            }
            catch (Exception ex)
            {
                commandResult = new CommandResult(126, startTime, DateTimeOffset.Now);
                commandExceptionOutput(ex.Message);
            }

            return new CommandResultPlus(commandResult, _standardOutput.ToString(), _errorOutput.ToString(), _exceptionOutput.ToString());
        }

        private Command prepareCommand(Command command) {
            return command.WithEnvironmentVariables(_debianEnvironmentVariables);
        }

        protected async Task<(bool success, string result)> RunAsync(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null)
        {
            return await RunAsync<string>(command, customStandardOutput, customErrorOutput);
        }

        protected async Task<(bool success, TReturn result)> RunAsync<TReturn>(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null)
        {
            var rawResult = await RunRawAsync(command, customStandardOutput, customErrorOutput);
            var success = rawResult.Result.ExitCode == 0;
            var resultString = success ? rawResult.StandardOutput : rawResult.ExceptionOutput.Length > 0 ? rawResult.ExceptionOutput : rawResult.ErrorOutput;
            var conversionTypeName = (Nullable.GetUnderlyingType(typeof(TReturn)) ?? typeof(TReturn)).Name;
            object result = conversionTypeName switch
            {
                nameof(String) => resultString,
                nameof(Int32) => int.TryParse(resultString, out int x) ? x : -1,
                _ => throw new NoCommandResultConversionToTypeException(conversionTypeName)
            };
            return (success, (TReturn)result);
        }

        public void SetCommandView(ICommandView view) {
            _instance._commandView = view;
        }

        public void SetOutput(CommandOutputType? overriddenOutputType) {
            _instance._overriddenOutputType = overriddenOutputType;
        }
    }
}
