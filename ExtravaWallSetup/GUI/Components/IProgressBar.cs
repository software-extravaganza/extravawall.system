namespace ExtravaWallSetup.GUI.Components {
    public interface IProgressBar {
        void Failed(string? message);
        void UpdateProgress(float progress, string? text = null);
    }
}