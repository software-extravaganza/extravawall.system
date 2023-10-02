using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;

namespace ExtravaCore.Commands;
public interface ICommandWrapperBase<TCommand, out TResult>
    where TCommand : ICommandWrapperBase<TCommand, TResult> { }

public abstract partial class CommandBase : ICommand {
    private ICommandDriver? _driver;
    private OperatingSystem? _os;
    public OperatingSystem OS {
        get { return _os ?? throw new InvalidOperationException("Operating System not set."); }
        set {
            _driver = value.CommandDriverFactory();
            _os = value;
        }
    }

    public ICommandDriver Driver {
        get { return _driver ?? throw new InvalidOperationException("Operating System not set."); }
    }

    public void SetCommandView(ICommandView view) {
        Driver.SetCommandView(view);
    }

    public void SetOutput(CommandOutputType? overriddenOutputType) {
        Driver.SetOutput(overriddenOutputType);
    }
}

public abstract class CommandWrapperBase<TCommand, TResult>
    : CommandBase, ICommandWrapperBase<TCommand, TResult>
    where TCommand : CommandWrapperBase<TCommand, TResult> {

    protected ICommandView? CommandView { get; set; }
    protected CommandOutputType? OverriddenOutputType { get; set; }


    protected async Task<ICommandResult<TCustomResult>> runCustomAsync<TCustomResult>(Command command, Func<string, TCustomResult>? conversionDelegate = null, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, conversionDelegate, customStandardOutput, customErrorOutput);
    }

    protected async Task<ICommandResult<TResult>> runAsync(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        return await Driver.RunAsync(command, convertResult, customStandardOutput, customErrorOutput);
    }

    protected abstract TResult convertResult(string result);
}

public interface ICommandWrapperWithOptions<TCommand, TOptions, TResult> : ICommandWrapperBase<TCommand, TResult>
    where TCommand : ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new() {
    Task<ICommandResult<TResult>> RunAsync(TOptions? options);
}

public abstract class CommandWrapperWithOptions<TCommand, TOptions, TResult> : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TCommand : CommandWrapperWithOptions<TCommand, TOptions, TResult>
    where TOptions : new() {

    public virtual async Task<ICommandResult<TResult>> RunAsync(TOptions? options) {
        options ??= new TOptions();
        var command = commandGenerator(options);
        return await runAsync(command);
    }

    protected abstract Command commandGenerator(TOptions result);
}

public abstract class CommandWrapperWithStringInput<TCommand, TResult> : CommandWrapperBase<TCommand, TResult>
    where TCommand : CommandWrapperWithStringInput<TCommand, TResult> {


    public virtual async Task<ICommandResult<TResult>> RunAsync(string input) {
        input ??= string.Empty;
        var command = commandGenerator(input);
        return await runAsync(command);
    }

    protected abstract Command commandGenerator(string input);
}

public interface ICommandWrapperWithNoInputResult<TCommand, TResult> : ICommandWrapperBase<TCommand, TResult>
    where TCommand : ICommandWrapperWithNoInputResult<TCommand, TResult> {
    Task<ICommandResult<TResult>> RunAsync();
}

public abstract class CommandWrapperWithNoInput<TCommand, TResult> : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithNoInputResult<TCommand, TResult>
    where TCommand : CommandWrapperWithNoInput<TCommand, TResult> {

    public virtual async Task<ICommandResult<TResult>> RunAsync() {
        var command = commandGenerator();
        return await runAsync(command);
    }

    protected abstract Command commandGenerator();
}


public abstract class CommandDriverBase<TCommand> : ICommandDriver
        where TCommand : CommandDriverBase<TCommand> {
    //public static T Instance => _instance;

    //private static readonly T _instance = new T();
    private readonly StringBuilder _standardOutput = new StringBuilder();
    private readonly StringBuilder _errorOutput = new StringBuilder();
    private readonly StringBuilder _exceptionOutput = new StringBuilder();
    private readonly IReadOnlyDictionary<string, string?> _debianEnvironmentVariables =
            new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?> { { "DEBIAN_FRONTEND", "noninteractive" } }
            );
    public CommandSettings Settings { get; private init; }

    protected ICommandView? CommandView { get; set; }
    protected CommandOutputType? OverriddenOutputType { get; set; }

    protected CommandDriverBase(CommandSettings settings) => Settings = settings;

    private void commandStandardOutput(string output) {
        _standardOutput.AppendLine(output);
        switch (OverriddenOutputType ?? Settings.OutputToVirtualConsole) {
            case CommandOutputType.Console:
                Console.WriteLine(output);
                break;
            case CommandOutputType.VirtualConsole:
                CommandView?.WriteStandardLine(output);
                break;
        }
            ;
    }

    private void commandErrorOutput(string output) {
        _errorOutput.AppendLine(output);
        switch (OverriddenOutputType ?? Settings.OutputToVirtualConsole) {
            case CommandOutputType.Console:
                Console.Error.WriteLine(output);
                break;
            case CommandOutputType.VirtualConsole:
                CommandView?.WriteErrorLine(output);
                break;
        }
            ;
    }

    private void commandExceptionOutput(string output) {
        _exceptionOutput.AppendLine(output);
        switch (OverriddenOutputType ?? Settings.OutputToVirtualConsole) {
            case CommandOutputType.Console:
                Console.Error.WriteLine(output);
                break;
            case CommandOutputType.VirtualConsole:
                CommandView?.WriteExceptionLine(output);
                break;
        }
    }

    public virtual async Task<ICommandResultRaw> RunRawAsync(Command command, Action<string>? customStandardOutput, Action<string>? customErrorOutput) {
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
        CommandResult? commandResult = null;
        int exitCode = -1;
        try {
            var result = await finalCommand.ExecuteAsync();
            exitCode = result.ExitCode;
            commandResult = (CommandResult?)result;
        } catch (Exception ex) {
            exitCode = 126;
            commandExceptionOutput(ex.Message);
        } finally {
            commandResult ??= new CommandResult(exitCode, startTime, DateTimeOffset.Now);
        }

        return new CommandResultRaw(
                        ExitCode: commandResult.ExitCode,
                        Result: _standardOutput.ToString(),
                        StartTime: commandResult.StartTime,
                        ExitTime: commandResult.ExitTime,
                        RunTime: commandResult.RunTime,
                        StandardOutput: _standardOutput.ToString(),
                        ErrorOutput: _errorOutput.ToString());

    }

    private Command prepareCommand(Command command) {
        return command.WithEnvironmentVariables(_debianEnvironmentVariables);
    }

    public virtual async Task<ICommandResult<TResult>> RunAsync<TResult>(Command command, Func<string, TResult>? conversionDelegate = null, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        var rawResult = await RunRawAsync(command, customStandardOutput, customErrorOutput);
        var success = rawResult.ExitCode == 0;
        conversionDelegate ??= (string s) => (TResult)Convert.ChangeType(s, typeof(TResult));

        var convertedResult = conversionDelegate.Invoke(rawResult.StandardOutput);
        var conversionTypeName = (
                Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult)
        ).Name;

        return new CommandResult<TResult>(rawResult.ExitCode, convertedResult, rawResult.StartTime, rawResult.ExitTime, rawResult.RunTime);
    }

    public virtual async Task<ICommandResult<string>> RunAsyncStdOut(Command command, Action<string>? customStandardOutput = null, Action<string>? customErrorOutput = null) {
        var rawResult = await RunRawAsync(command, customStandardOutput, customErrorOutput);
        var success = rawResult.ExitCode == 0;
        var resultString = success
                ? rawResult.StandardOutput
                : rawResult.ExceptionOutput.Length > 0
                        ? rawResult.ExceptionOutput
                        : rawResult.ErrorOutput;


        return new CommandResult<string>(rawResult.ExitCode, resultString, rawResult.StartTime, rawResult.ExitTime, rawResult.RunTime);
    }

    public virtual void SetCommandView(ICommandView view) {
        CommandView = view;
    }

    public virtual void SetOutput(CommandOutputType? overriddenOutputType) {
        OverriddenOutputType = overriddenOutputType;
    }

    public abstract Task<ICommandResult<DirectoryInfo>> GetProgramLocationAsync(string program);
}
