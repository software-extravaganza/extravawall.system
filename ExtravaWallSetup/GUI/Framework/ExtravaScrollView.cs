using Terminal.Gui;

namespace ExtravaWallSetup.GUI.Framework;

[System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ExtravaScrollView : ScrollView {
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => ToString();

    private ExtravaScrollView? findScrollView(View parent) {
        if (parent is ExtravaScrollView scrollView) {
            return scrollView;
        } else if (parent.SuperView != null) {
            return findScrollView(parent.SuperView);
        }

        return null;
    }

    public override bool ProcessHotKey(KeyEvent keyEvent) {
        if (keyEvent.Key == Key.CursorUp) {
            _ = (findScrollView(this)?.ScrollUp(1));
        } else if (keyEvent.Key == Key.CursorDown) {
            _ = (findScrollView(this)?.ScrollDown(1));
        } else if (keyEvent.Key == Key.CursorRight) {
            _ = (findScrollView(this)?.ScrollRight(1));
        } else if (keyEvent.Key == Key.CursorLeft) {
            _ = (findScrollView(this)?.ScrollLeft(1));
        }

        return base.ProcessHotKey(keyEvent);
    }
}
