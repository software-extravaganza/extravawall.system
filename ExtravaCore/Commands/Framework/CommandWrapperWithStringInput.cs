using CliWrap;

namespace ExtravaCore.Commands.Framework {
    public abstract class CommandWrapperWithStringInput<TCommand, TResult> : CommandWrapperBase<TCommand, TResult>
        where TCommand : CommandWrapperWithStringInput<TCommand, TResult> {


        public virtual async Task<ICommandResult<TResult>> RunAsync(string input) {
            input ??= string.Empty;
            var command = commandGenerator(input);
            return await runAsync(command);
        }

        protected abstract Command commandGenerator(string input);
    }
}