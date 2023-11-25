using NStack;

namespace ExtravaWallSetup.GUI.Components {
    public interface ITextOutput {
        void Write(ustring output);
        void WriteLine(ustring? output = null);
    }
}