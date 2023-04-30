namespace ExtravaCore.Commands;
public interface ICommandResult {
    bool Success { get => ExitCode == 0; }
    string? Result { get; }
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

public class CommandResult : ICommandResult {

    public bool Success { get; }
    public string? Result { get; }

    public int ExitCode { get; }
    public DateTimeOffset StartTime { get; }

    public DateTimeOffset ExitTime { get; }

    public TimeSpan RunTime { get; }

    public CommandResult(int exitCode, string? result, DateTimeOffset startTime, DateTimeOffset exitTime) {
        this.ExitTime = exitTime;
        this.StartTime = startTime;
        this.RunTime = exitTime - startTime;
        ExitCode = exitCode;
        Result = result;
    }

    public CommandResult(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime) {
        this.ExitTime = exitTime;
        this.StartTime = startTime;
        this.RunTime = exitTime - startTime;
        ExitCode = exitCode;
    }

    public CommandResult(int exitCode, DateTimeOffset exitTime, TimeSpan runTime) {
        this.ExitCode = exitCode;
        this.ExitTime = exitTime;
        this.RunTime = runTime;
    }

    public CommandResult(int exitCode, string? result, DateTimeOffset startTime, TimeSpan runTime) {
        this.RunTime = runTime;
        this.StartTime = startTime;
        this.ExitTime = startTime + runTime;
        ExitCode = exitCode;
        Result = result;
    }

    public static implicit operator CommandResult(CliWrap.CommandResult result) {
        return new CommandResult(result.ExitCode, result.StartTime, result.ExitTime);
    }
}