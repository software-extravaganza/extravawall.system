using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Terminal.Gui.Graphs.BarSeries;
using Terminal.Gui.Graphs;
using Terminal.Gui;

namespace ExtravaWallSetup.GUI {
    class DiscoBarSeries : BarSeries {
        private Terminal.Gui.Attribute green;
        private Terminal.Gui.Attribute brightgreen;
        private Terminal.Gui.Attribute brightyellow;
        private Terminal.Gui.Attribute red;
        private Terminal.Gui.Attribute brightred;

        public DiscoBarSeries() {

            green = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black);
            brightgreen = Application.Driver.MakeAttribute(Color.Green, Color.Black);
            brightyellow = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
            red = Application.Driver.MakeAttribute(Color.Red, Color.Black);
            brightred = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black);
        }
        protected override void DrawBarLine(GraphView graph, Terminal.Gui.Point start, Terminal.Gui.Point end, Bar beingDrawn) {
            var driver = Application.Driver;

            int x = start.X;
            for (int y = end.Y; y <= start.Y; y++) {

                var height = graph.ScreenToGraphSpace(x, y).Y;

                if (height >= .85) {
                    driver.SetAttribute(red);
                }
                else if (height >= .66) {
                    driver.SetAttribute(brightred);
                }
                else if (height >= .45) {
                    driver.SetAttribute(brightyellow);
                }
                else if (height >= .25) {
                    driver.SetAttribute(brightgreen);
                }
                else {
                    driver.SetAttribute(green);
                }

                graph.AddRune(x, y, beingDrawn.Fill.Rune);
            }
        }
    }
}
