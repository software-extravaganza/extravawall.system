using NStack;

namespace ExtravaWallSetup.GUI {
    public interface ITextOutput {
        void Write(ustring output);
        void WriteLine(ustring output = null);
    }
}