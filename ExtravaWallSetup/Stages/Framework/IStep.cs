namespace ExtravaWallSetup.Stages.Framework {
    public interface IStep {
        StageType Stage { get; }
        short StepOrder { get; }
        string Name { get; }
        bool IsComplete { get; }
        bool IsExecuting { get; }

        Task StepTask { get; }
        Task BeginExecute();
        Task BeginComplete();
        void Initialize(InstallManager installManager);
    }
}