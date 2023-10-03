using System.Text;

namespace ExtravaCore.Commands.Framework;
public interface ICommandResultRaw : ICommandResultBase {
    int ExitCode { get; set; }
    string CommandString { get; set; }
    string StandardOutput { get; }
    string ErrorOutput { get; }
    string ExceptionOutput { get; }

    ICommandResult<TNew> ToNewCommandResultWithValue<TNew>(TNew newResult);
}

public record CommandResultRaw(
    string CommandString,
    int ExitCode,
    bool IsSuccess,
    DateTimeOffset StartTime,
    DateTimeOffset ExitTime,
    TimeSpan RunTime,
    string StandardOutput = "",
    string ErrorOutput = "",
    string ExceptionOutput = "") : ICommandResultRaw {
    public bool IsSuccess { get; set; }
    public int ExitCode { get; set; }
    public string CommandString { get; set; }
    public CommandResultRaw(string commandString, int exitCode, bool isSuccess, DateTimeOffset startTime, DateTimeOffset exitTime) : this(commandString, exitCode, isSuccess, startTime, exitTime, exitTime - startTime) { }
    public CommandResultRaw(string commandString, int exitCode, bool isSuccess, DateTimeOffset exitTime, TimeSpan runTime) : this(commandString, exitCode, isSuccess, exitTime - runTime, exitTime, runTime) { }
    // public CommandResultRaw(string commandString, ICommandResult Result, string StandardOutput = "", string ErrorOutput = "", string ExceptionOutput = "") : this(commandString, Result.RawResult.ExitCode, Result.Result, Result.StartTime, Result.StartTime + Result.RunTime, Result.RunTime, StandardOutput, ErrorOutput, ExceptionOutput) { }

    public ICommandResult<TNew> ToNewCommandResultWithValue<TNew>(TNew newResult) => new CommandResult<TNew>(this.CommandString, this, newResult, this.IsSuccess, this.StartTime, this.ExitTime, this.RunTime);

}

public static class CliWrapCommandResultExtensions {
    public static ICommandResultRaw ToCommandResultRaw(this CliWrap.CommandResult commandResult, string commandString, StringBuilder _standardOutput, StringBuilder _errorOutput)
    => new CommandResultRaw(commandString,
            commandResult.ExitCode,
            commandResult.ExitCode == 0,
            commandResult.StartTime,
            commandResult.ExitTime,
            commandResult.ExitTime - commandResult.StartTime,
            _standardOutput.ToString(), _errorOutput.ToString());
}