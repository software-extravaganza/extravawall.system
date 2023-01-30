using CliWrap;

namespace ExtravaWallSetup.Commands.Framework
{
    public record CommandResultPlus(CommandResult Result, string StandardOutput = "", string ErrorOutput = "", string ExceptionOutput = "");
}
