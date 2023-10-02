
//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      You can make changes to this file and they will not be overwritten when saving.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI.Components {
    using System.Diagnostics;
    using ExtravaWallSetup.GUI.Framework;
    using Terminal.Gui;


    public partial class ElevateMenuView {
        bool _pendingClick;
        private InstallManager _installManager;
        private TaskCompletionSource<int> _choiceTask = new TaskCompletionSource<int>();
        private readonly ITextOutput _writer;
        public Task ChoiceMade => _choiceTask.Task;
        public ElevateMenuView(InstallManager installManager, ITextOutput writer = null) {
            _installManager = installManager;
            _writer = writer;
            InitializeComponent();
            this.listView.MouseClick += ListView_MouseClick;
            this.listView.KeyPress += ListView_KeyPress;
            this.DrawContentComplete += StartMenuView_DrawContentCompleteAsync;
            this.listView.SetFocus();
        }

        private async void StartMenuView_DrawContentCompleteAsync(Rect obj) {
            var shouldRun = _pendingClick;
            _pendingClick = false;
            if (shouldRun) {
                await Run(this.listView.SelectedItem);
            }
        }

        private void ListView_KeyPress(KeyEventEventArgs args) {
            if (args.KeyEvent.Key == Key.Enter) {
                _pendingClick = true;
            }
        }

        private void ListView_MouseClick(MouseEventArgs args) {
            _pendingClick = true;
        }

        private async Task Run(int selectedItem) {
            this.listView.Enabled = false;
            switch (selectedItem) {
                case 0:
                    Application.Shutdown();
                    // await _installManager.RestartAndRunElevated(() => {
                    //     // Shutdown the Terminal.Gui application
                    //     _installManager.Dispose();
                    //     _choiceTask.SetResult(selectedItem);
                    // });

                    break;
                case 1:
                    _installManager.RequestEndOnNextStep("Installation requires an ELEVATED execution (sudo make me a sandwich).");
                    _choiceTask.SetResult(selectedItem);
                    break;

            }
            await Task.CompletedTask;
        }
    }
}
