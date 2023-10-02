
//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      You can make changes to this file and they will not be overwritten when saving.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI.Components {
    using CliWrap;
    using ExtravaCore.Commands;
    using ExtravaWallSetup.GUI.Framework;
    using System.Linq.Expressions;
    using Terminal.Gui;


    public partial class CommandView : ICommandView {

        int currentWidth = 0, currentHeight = 0;

        public CommandView() {
            InitializeComponent();
            commandResult.Text = string.Empty;
        }

        public void WriteErrorLine(string output) {
            printOut(output);
        }

        public void WriteExceptionLine(string output) {
            printOut(output);
        }

        public void WriteStandardLine(string output) {
            printOut(output);
        }

        private void printOut(string output) {
            commandResult.Text = commandResult.Text + output + Environment.NewLine;
            var countOfLines = commandResult.Text.ToString().Count(t => t == '\n');
            currentWidth = currentWidth > output.Length ? currentWidth : output.Length;
            currentHeight = countOfLines;
            Width = commandResult.Width = currentWidth;
            Height = commandResult.Height = currentHeight;
            InstallManager.Instance.Console.RefreshConsole(this);
        }
    }
}
