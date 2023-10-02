namespace ExtravaCore.Commands;
public interface ICommandResultRaw : ICommandResult {
    string StandardOutput { get; }
    string ErrorOutput { get; }
    string ExceptionOutput { get; }
}

public record CommandResultRaw(
    int ExitCode,
    string? Result,
    DateTimeOffset StartTime,
    DateTimeOffset ExitTime,
    TimeSpan RunTime,
    string StandardOutput = "",
    string ErrorOutput = "",
    string ExceptionOutput = "") : ICommandResultRaw {
    public bool Success => ExitCode == 0;

    public CommandResultRaw(int exitCode, string? result, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, result, startTime, exitTime, exitTime - startTime) { }

    public CommandResultRaw(int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime) : this(exitCode, null, startTime, exitTime, exitTime - startTime) { }

    public CommandResultRaw(int exitCode, DateTimeOffset exitTime, TimeSpan runTime) : this(exitCode, null, exitTime - runTime, exitTime, runTime) { }

    public CommandResultRaw(int exitCode, string? result, DateTimeOffset startTime, TimeSpan runTime) : this(exitCode, result, startTime, startTime + runTime, runTime) { }

    public CommandResultRaw(ICommandResult Result, string StandardOutput = "", string ErrorOutput = "", string ExceptionOutput = "") : this(Result.ExitCode, Result.Result, Result.StartTime, Result.StartTime + Result.RunTime, Result.RunTime, StandardOutput, ErrorOutput, ExceptionOutput) { }


    public static implicit operator CommandResultRaw(CliWrap.CommandResult result) => new(result.ExitCode, result.StartTime, result.ExitTime);
}