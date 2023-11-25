using CliWrap;

namespace ExtravaCore.Commands.Framework {
    public abstract class CommandWrapperWithStringInput<TResult> : CommandWrapperBase<TResult> {


        // public virtual async Task<ICommandResult<TResult>> RunAsync(string input) {
        //     input ??= string.Empty;
        //     var command = commandGenerator(input);
        //     return await runAsync(command);
        // }

        protected abstract Command commandGenerator(string input);
    }
}