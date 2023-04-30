using CliWrap;

namespace ExtravaCore.Commands;
public record CommandResultPlus(CommandResult Result, string StandardOutput = "", string ErrorOutput = "", string ExceptionOutput = "");
