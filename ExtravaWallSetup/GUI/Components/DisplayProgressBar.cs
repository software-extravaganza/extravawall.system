using System;

//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      You can make changes to this file and they will not be overwritten when saving.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI.Components {
    using CliWrap;
    using ExtravaWallSetup.GUI.Framework;
    using NStack;
    using System.Linq.Expressions;
    using Terminal.Gui;


    public partial class DisplayProgressBar : IProgressBar {
        public DisplayProgressBar(float initialProgress, string? text = null) {
            InitializeComponent();
            UpdateProgress(initialProgress, text);
        }

        public void Failed(string? message) {
            progressMessage.Text = message;
            progressMessage.ColorScheme = this.redOnBlack;

        }

        public void UpdateProgress(float progress, string? text = null) {
            progressMessage.Text = text ?? progressMessage.Text;
            progressBar.Fraction = progress;
            progressBar.Text = progress.ToString("P");
        }
    }
}
