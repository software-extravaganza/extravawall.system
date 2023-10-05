namespace ExtravaCore.Commands.Framework;
public interface ICommandWrapperWithNoInputResult : ICommandWrapperBase { }
public interface ICommandWrapperWithNoInputResult<TResult> : ICommandWrapperBase<TResult>, ICommandWrapperWithNoInputResult {
    //Task<ICommandResult<TResult>> RunAsync();
}
