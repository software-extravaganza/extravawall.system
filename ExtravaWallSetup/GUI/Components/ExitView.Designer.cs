
//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI {
    using System;
    using Terminal.Gui;
    
    
    public partial class ExitView : Terminal.Gui.View {
        
        private Terminal.Gui.ColorScheme redOnBlackCustom;
        
        private Terminal.Gui.ColorScheme blackOnRedCustom;
        
        private Terminal.Gui.ColorScheme greenOnBlackCustom;
        
        private Terminal.Gui.ColorScheme blackOnGreenCustom;
        
        private Terminal.Gui.Label titleLabel;
        
        private Terminal.Gui.TextView contentLabel;
        
        private Terminal.Gui.Button exitButton;
        
        private void InitializeComponent() {
            this.exitButton = new Terminal.Gui.Button();
            this.contentLabel = new Terminal.Gui.TextView();
            this.titleLabel = new Terminal.Gui.Label();
            this.redOnBlackCustom = new Terminal.Gui.ColorScheme();
            this.redOnBlackCustom.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Black);
            this.redOnBlackCustom.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Black);
            this.redOnBlackCustom.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Brown);
            this.redOnBlackCustom.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Brown);
            this.redOnBlackCustom.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black);
            this.blackOnRedCustom = new Terminal.Gui.ColorScheme();
            this.blackOnRedCustom.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Red);
            this.blackOnRedCustom.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Red);
            this.blackOnRedCustom.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Red);
            this.blackOnRedCustom.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Red);
            this.blackOnRedCustom.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Red);
            this.greenOnBlackCustom = new Terminal.Gui.ColorScheme();
            this.greenOnBlackCustom.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Black);
            this.greenOnBlackCustom.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Black);
            this.greenOnBlackCustom.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Brown);
            this.greenOnBlackCustom.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Brown);
            this.greenOnBlackCustom.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black);
            this.blackOnGreenCustom = new Terminal.Gui.ColorScheme();
            this.blackOnGreenCustom.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.BrightGreen);
            this.blackOnGreenCustom.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.BrightGreen);
            this.blackOnGreenCustom.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.BrightGreen);
            this.blackOnGreenCustom.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.BrightGreen);
            this.blackOnGreenCustom.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.BrightGreen);
            this.Width = 75;
            this.Height = 10;
            this.X = 0;
            this.Y = 0;
            this.ColorScheme = this.blackOnRedCustom;
            this.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.titleLabel.Width = Dim.Fill(1);
            this.titleLabel.Height = 1;
            this.titleLabel.X = 1;
            this.titleLabel.Y = 2;
            this.titleLabel.ColorScheme = this.redOnBlackCustom;
            this.titleLabel.Data = "titleLabel";
            this.titleLabel.Text = "";
            this.titleLabel.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.Add(this.titleLabel);
            this.contentLabel.Width = Dim.Fill(1);
            this.contentLabel.Height = Dim.Fill(3);
            this.contentLabel.X = 2;
            this.contentLabel.Y = Pos.Bottom(titleLabel) + 1;
            this.contentLabel.AllowsTab = true;
            this.contentLabel.AllowsReturn = true;
            this.contentLabel.WordWrap = false;
            this.contentLabel.Data = "contentLabel";
            this.contentLabel.Text = "";
            this.contentLabel.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.Add(this.contentLabel);
            this.exitButton.Width = 18;
            this.exitButton.Height = 1;
            this.exitButton.X = 0;
            this.exitButton.Y = Pos.Bottom(contentLabel) + 1;
            this.exitButton.Data = "exitButton";
            this.exitButton.Text = "Exit Installer";
            this.exitButton.TextAlignment = Terminal.Gui.TextAlignment.Centered;
            this.exitButton.IsDefault = false;
            this.Add(this.exitButton);
        }
    }
}
