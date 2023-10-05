using CliWrap;

namespace ExtravaCore.Commands.Framework {
    public abstract class CommandWrapperWithNoInput<TCommand, TResult> : CommandWrapperBase<TCommand, TResult>, ICommandWrapperWithNoInputResult<TCommand, TResult>
        where TCommand : CommandWrapperWithNoInput<TCommand, TResult> {

        // public virtual async Task<ICommandResult<TResult>> RunAsync() {
        //     var command = commandGenerator();
        //     return await runAsync(command);
        // }

        protected abstract Command commandGenerator();
    }
}