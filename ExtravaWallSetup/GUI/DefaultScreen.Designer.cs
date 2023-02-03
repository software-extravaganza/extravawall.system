
//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI {
    using ExtravaWallSetup.GUI.Framework;
    using System;
    using Terminal.Gui;
    
    
    public partial class DefaultScreen : Terminal.Gui.Toplevel {
        
        private Terminal.Gui.ColorScheme redOnBlack;
        
        private Terminal.Gui.ColorScheme blueOnBlack;
        
        private Terminal.Gui.ColorScheme greenOnBlack;
        
        private Terminal.Gui.ColorScheme whiteOnBlue;
        
        private Terminal.Gui.LineView lineView;
        
        private Terminal.Gui.Label titleLabel;
        
        private Terminal.Gui.FrameView consoleView;
        
        private ExtravScrollView consoleScrollView;
        
        private Terminal.Gui.FrameView infoFrame;
        
        private Terminal.Gui.TableView infoTable;
        
        private Terminal.Gui.GraphView cpuGraph;
        
        private Terminal.Gui.Label cpuGraphLabel;
        
        private Terminal.Gui.GraphView memGraph;
        
        private Terminal.Gui.Label memGraphLabel;
        
        private void InitializeComponent() {
            this.memGraphLabel = new Terminal.Gui.Label();
            this.memGraph = new Terminal.Gui.GraphView();
            this.cpuGraphLabel = new Terminal.Gui.Label();
            this.cpuGraph = new Terminal.Gui.GraphView();
            this.infoTable = new Terminal.Gui.TableView();
            this.infoFrame = new Terminal.Gui.FrameView();
            this.consoleScrollView = new ExtravScrollView();
            this.consoleView = new Terminal.Gui.FrameView();
            this.titleLabel = new Terminal.Gui.Label();
            this.lineView = new Terminal.Gui.LineView();
            this.redOnBlack = new Terminal.Gui.ColorScheme();
            this.redOnBlack.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.Red, Terminal.Gui.Color.Black);
            this.redOnBlack.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Black);
            this.redOnBlack.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.Red, Terminal.Gui.Color.Brown);
            this.redOnBlack.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightRed, Terminal.Gui.Color.Brown);
            this.redOnBlack.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black);
            this.blueOnBlack = new Terminal.Gui.ColorScheme();
            this.blueOnBlack.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightBlue, Terminal.Gui.Color.Black);
            this.blueOnBlack.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.Cyan, Terminal.Gui.Color.Black);
            this.blueOnBlack.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightBlue, Terminal.Gui.Color.BrightYellow);
            this.blueOnBlack.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.Cyan, Terminal.Gui.Color.BrightYellow);
            this.blueOnBlack.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black);
            this.greenOnBlack = new Terminal.Gui.ColorScheme();
            this.greenOnBlack.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.Green, Terminal.Gui.Color.Black);
            this.greenOnBlack.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Black);
            this.greenOnBlack.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.Green, Terminal.Gui.Color.Magenta);
            this.greenOnBlack.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightGreen, Terminal.Gui.Color.Magenta);
            this.greenOnBlack.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black);
            this.whiteOnBlue = new Terminal.Gui.ColorScheme();
            this.whiteOnBlue.Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue);
            this.whiteOnBlue.HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue);
            this.whiteOnBlue.Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue);
            this.whiteOnBlue.HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue);
            this.whiteOnBlue.Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue);
            this.Width = Dim.Fill(0);
            this.Height = Dim.Fill(0);
            this.X = 0;
            this.Y = 0;
            this.ColorScheme = this.blueOnBlack;
            this.Modal = false;
            this.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.lineView.Width = Dim.Fill(0);
            this.lineView.Height = 1;
            this.lineView.X = 0;
            this.lineView.Y = 0;
            this.lineView.ColorScheme = this.redOnBlack;
            this.lineView.Data = "lineView";
            this.lineView.TextAlignment = Terminal.Gui.TextAlignment.Right;
            this.lineView.LineRune = '█';
            this.lineView.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
            this.Add(this.lineView);
            this.titleLabel.Width = 30;
            this.titleLabel.Height = 1;
            this.titleLabel.X = Pos.Center();
            this.titleLabel.Y = 0;
            this.titleLabel.ColorScheme = this.redOnBlack;
            this.titleLabel.Data = "titleLabel";
            this.titleLabel.Text = "ExtravaWall Installer";
            this.titleLabel.TextAlignment = Terminal.Gui.TextAlignment.Centered;
            this.Add(this.titleLabel);
            this.consoleView.Width = Dim.Percent(70f);
            this.consoleView.Height = Dim.Fill(0);
            this.consoleView.X = Pos.Left(lineView);
            this.consoleView.Y = Pos.Bottom(lineView) + 1;
            this.consoleView.ColorScheme = this.greenOnBlack;
            this.consoleView.Data = "consoleView";
            this.consoleView.Border.BorderStyle = Terminal.Gui.BorderStyle.Single;
            this.consoleView.Border.BorderBrush = Terminal.Gui.Color.Black;
            this.consoleView.Border.Effect3D = false;
            this.consoleView.Border.Effect3DBrush = null;
            this.consoleView.Border.DrawMarginFrame = true;
            this.consoleView.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.consoleView.Title = "Console";
            this.Add(this.consoleView);
            this.consoleScrollView.Width = Dim.Fill(0);
            this.consoleScrollView.Height = Dim.Fill(0);
            this.consoleScrollView.X = 0;
            this.consoleScrollView.Y = 0;
            this.consoleScrollView.ContentSize = new Size(20,10);
            this.consoleScrollView.Data = "consoleScrollView";
            this.consoleScrollView.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.consoleView.Add(this.consoleScrollView);
            this.infoFrame.Width = Dim.Percent(30f);
            this.infoFrame.Height = Dim.Fill(0);
            this.infoFrame.X = Pos.Right(consoleView);
            this.infoFrame.Y = Pos.Top(consoleView);
            this.infoFrame.Data = "infoFrame";
            this.infoFrame.Border.BorderStyle = Terminal.Gui.BorderStyle.Single;
            this.infoFrame.Border.BorderBrush = Terminal.Gui.Color.Black;
            this.infoFrame.Border.Effect3D = false;
            this.infoFrame.Border.Effect3DBrush = null;
            this.infoFrame.Border.DrawMarginFrame = true;
            this.infoFrame.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.infoFrame.Title = "Info";
            this.Add(this.infoFrame);
            this.infoTable.Width = Dim.Percent(100f);
            this.infoTable.Height = Dim.Percent(45f);
            this.infoTable.X = 0;
            this.infoTable.Y = 1;
            this.infoTable.ColorScheme = this.whiteOnBlue;
            this.infoTable.Data = "infoTable";
            this.infoTable.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.infoTable.FullRowSelect = false;
            this.infoTable.Style.AlwaysShowHeaders = false;
            this.infoTable.Style.ExpandLastColumn = true;
            this.infoTable.Style.InvertSelectedCellFirstCharacter = false;
            this.infoTable.Style.ShowHorizontalHeaderOverline = false;
            this.infoTable.Style.ShowHorizontalHeaderUnderline = true;
            this.infoTable.Style.ShowVerticalCellLines = false;
            this.infoTable.Style.ShowVerticalHeaderLines = false;
            System.Data.DataTable infoTableTable;
            infoTableTable = new System.Data.DataTable();
            System.Data.DataColumn infoTableTableProperty;
            infoTableTableProperty = new System.Data.DataColumn();
            infoTableTableProperty.ColumnName = "Property";
            infoTableTable.Columns.Add(infoTableTableProperty);
            System.Data.DataColumn infoTableTableValue;
            infoTableTableValue = new System.Data.DataColumn();
            infoTableTableValue.ColumnName = "Value";
            infoTableTable.Columns.Add(infoTableTableValue);
            this.infoTable.Table = infoTableTable;
            this.infoFrame.Add(this.infoTable);
            this.cpuGraph.Width = Dim.Percent(100f);
            this.cpuGraph.Height = 7;
            this.cpuGraph.X = Pos.Left(infoTable);
            this.cpuGraph.Y = Pos.Bottom(infoTable) + 1;
            this.cpuGraph.Data = "cpuGraph";
            this.cpuGraph.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.cpuGraph.GraphColor = Terminal.Gui.Attribute.Make(Color.White,Color.Blue);
            this.cpuGraph.ScrollOffset = new Terminal.Gui.PointF(0F, 0F);
            this.cpuGraph.MarginLeft = 0u;
            this.cpuGraph.MarginBottom = 0u;
            this.cpuGraph.CellSize = new Terminal.Gui.PointF(1F, 0.2F);
            this.cpuGraph.AxisX.Visible = false;
            this.cpuGraph.AxisX.Increment = 1F;
            this.cpuGraph.AxisX.ShowLabelsEvery = 5u;
            this.cpuGraph.AxisX.Minimum = 1F;
            this.cpuGraph.AxisX.Text = "CPU";
            this.cpuGraph.AxisY.Visible = true;
            this.cpuGraph.AxisY.Increment = 0.2F;
            this.cpuGraph.AxisY.ShowLabelsEvery = 0u;
            this.cpuGraph.AxisY.Minimum = null;
            this.cpuGraph.AxisY.Text = null;
            this.infoFrame.Add(this.cpuGraph);
            this.cpuGraphLabel.Width = 3;
            this.cpuGraphLabel.Height = 1;
            this.cpuGraphLabel.X = Pos.Center();
            this.cpuGraphLabel.Y = Pos.Top(cpuGraph) + 1;
            this.cpuGraphLabel.Data = "cpuGraphLabel";
            this.cpuGraphLabel.Text = "CPU";
            this.cpuGraphLabel.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.infoFrame.Add(this.cpuGraphLabel);
            this.memGraph.Width = Dim.Percent(100f);
            this.memGraph.Height = 7;
            this.memGraph.X = Pos.Left(cpuGraph);
            this.memGraph.Y = Pos.Bottom(cpuGraph) + 1;
            this.memGraph.Data = "memGraph";
            this.memGraph.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.memGraph.GraphColor = Terminal.Gui.Attribute.Make(Color.White,Color.Blue);
            this.memGraph.ScrollOffset = new Terminal.Gui.PointF(0F, 0F);
            this.memGraph.MarginLeft = 0u;
            this.memGraph.MarginBottom = 0u;
            this.memGraph.CellSize = new Terminal.Gui.PointF(1F, 0.2F);
            this.memGraph.AxisX.Visible = false;
            this.memGraph.AxisX.Increment = 1F;
            this.memGraph.AxisX.ShowLabelsEvery = 5u;
            this.memGraph.AxisX.Minimum = 1F;
            this.memGraph.AxisX.Text = "Mem";
            this.memGraph.AxisY.Visible = true;
            this.memGraph.AxisY.Increment = 0.2F;
            this.memGraph.AxisY.ShowLabelsEvery = 0u;
            this.memGraph.AxisY.Minimum = null;
            this.memGraph.AxisY.Text = null;
            this.infoFrame.Add(this.memGraph);
            this.memGraphLabel.Width = 3;
            this.memGraphLabel.Height = 1;
            this.memGraphLabel.X = Pos.Center();
            this.memGraphLabel.Y = Pos.Top(memGraph) + 1;
            this.memGraphLabel.Data = "memGraphLabel";
            this.memGraphLabel.Text = "Mem";
            this.memGraphLabel.TextAlignment = Terminal.Gui.TextAlignment.Left;
            this.infoFrame.Add(this.memGraphLabel);
        }
    }
}
