using CliWrap;

namespace ExtravaCore.Commands.Framework {
    public abstract class CommandWrapperWithNoInput<TResult> : CommandWrapperBase<TResult>, ICommandWrapperWithNoInputResult<TResult> {

        public virtual async Task<ICommandResult<TResult>> runAsync() {
            var command = commandGenerator();
            return await driverRunAsync(command);
        }

        protected abstract Command commandGenerator();

        public override async Task<ICommandResult<TResult>> ExecuteAsync() {
            return await runAsync();
        }
    }
}