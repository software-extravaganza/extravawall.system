using System;
using CliWrap;

namespace ExtravaCore.Commands.Framework;
public interface ICommandResult : ICommandResult<string> { }
public interface ICommandResultBase : ICommandResultBase<string> { }
public interface ICommandResult<out T> : ICommandResultBase<T> {
    ICommandResultRaw? RawResult { get; }
    ICommandResult<TNew> ToNewCommandResultWithValue<TNew>(TNew newResult);
    T? Result { get; }
}

public interface ICommandResultBase<out T> {
    bool IsSuccess { get; set; }
    string CommandString { get; }
    //
    // Summary:
    //     When the command started executing.
    DateTimeOffset StartTime { get; }
    //
    // Summary:
    //     When the command finished executing.
    DateTimeOffset ExitTime { get; }
    //
    // Summary:
    //     Total duration of the command execution.
    TimeSpan RunTime { get; }
}

public record CommandResult(string CommandString, ICommandResultRaw? RawResult, string? Result, bool IsSuccess, DateTimeOffset StartTime, DateTimeOffset ExitTime, TimeSpan RunTime) : CommandResult<string>(CommandString, RawResult, Result, IsSuccess, StartTime, ExitTime, RunTime) {
    public CommandResult(string commandString, ICommandResultRaw? rawResult, string? result, bool isSuccess, DateTimeOffset startTime, DateTimeOffset exitTime) : this(commandString, rawResult, result, isSuccess, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, bool isSuccess, DateTimeOffset startTime, DateTimeOffset exitTime) : this(commandString, rawResult, default, isSuccess, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, bool isSuccess, TimeSpan runTime, DateTimeOffset exitTime) : this(commandString, rawResult, default, isSuccess, exitTime - runTime, exitTime, runTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, string? result, bool isSuccess, DateTimeOffset startTime, TimeSpan runTime) : this(commandString, rawResult, result, isSuccess, startTime, startTime + runTime, runTime) { }

    public static implicit operator CommandResult(CliWrap.CommandResult result) => new(string.Empty, null, result.ExitCode == 0, result.StartTime, result.ExitTime);
}

public record CommandResult<T>(string CommandString, ICommandResultRaw? RawResult, T? Result, bool IsSuccess, DateTimeOffset StartTime, DateTimeOffset ExitTime, TimeSpan RunTime) : ICommandResult<T> {
    public bool IsSuccess { get; set; }
    public CommandResult(string commandString, ICommandResultRaw? rawResult, T? result, bool isSuccess, DateTimeOffset startTime, DateTimeOffset exitTime) : this(commandString, rawResult, result, isSuccess, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, bool isSuccess, DateTimeOffset startTime, DateTimeOffset exitTime) : this(commandString, rawResult, default, isSuccess, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, bool isSuccess, TimeSpan runTime, DateTimeOffset exitTime) : this(commandString, rawResult, default, isSuccess, exitTime - runTime, exitTime, runTime) { }

    public CommandResult(string commandString, ICommandResultRaw? rawResult, T? result, bool isSuccess, DateTimeOffset startTime, TimeSpan runTime) : this(commandString, rawResult, result, isSuccess, startTime, startTime + runTime, runTime) { }

    public static implicit operator CommandResult<T>(CliWrap.CommandResult result) => new(string.Empty, null, result.ExitCode == 0, result.StartTime, result.ExitTime);

    public ICommandResult<TNew> ToNewCommandResultWithValue<TNew>(TNew newResult) => new CommandResult<TNew>(this.CommandString, this.RawResult, newResult, this.IsSuccess, this.StartTime, this.ExitTime, this.RunTime);
}