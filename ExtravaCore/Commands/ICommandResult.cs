using System;
namespace ExtravaCore.Commands;
public interface ICommandResult : ICommandResult<string> { }
public interface ICommandResult<out T> {
    bool Success { get => ExitCode == 0; }
    T? Result { get; }
    int ExitCode { get; }
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

public record CommandResult(int ExitCode, string? Result, DateTimeOffset StartTime, DateTimeOffset ExitTime, TimeSpan RunTime) : CommandResult<string>(ExitCode, Result, StartTime, ExitTime, RunTime) {
    public CommandResult(int exitCode, string? result, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, result, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, default, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(int exitCode, TimeSpan runTime, DateTimeOffset exitTime) : this(exitCode, default, exitTime - runTime, exitTime, runTime) { }

    public CommandResult(int exitCode, string? result, DateTimeOffset startTime, TimeSpan runTime) : this(exitCode, result, startTime, startTime + runTime, runTime) { }

    public static implicit operator CommandResult(CliWrap.CommandResult result) => new(result.ExitCode, result.StartTime, result.ExitTime);
}

public record CommandResult<T>(int ExitCode, T? Result, DateTimeOffset StartTime, DateTimeOffset ExitTime, TimeSpan RunTime) : ICommandResult<T> {
    public bool Success => ExitCode == 0;

    public CommandResult(int exitCode, T? result, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, result, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, default, startTime, exitTime, exitTime - startTime) { }

    public CommandResult(int exitCode, TimeSpan runTime, DateTimeOffset exitTime) : this(exitCode, default, exitTime - runTime, exitTime, runTime) { }

    public CommandResult(int exitCode, T? result, DateTimeOffset startTime, TimeSpan runTime) : this(exitCode, result, startTime, startTime + runTime, runTime) { }

    public static implicit operator CommandResult<T>(CliWrap.CommandResult result) => new(result.ExitCode, result.StartTime, result.ExitTime);
}