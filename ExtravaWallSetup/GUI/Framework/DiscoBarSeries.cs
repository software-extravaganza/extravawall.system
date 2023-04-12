using Terminal.Gui.Graphs;
using Terminal.Gui;

namespace ExtravaWallSetup.GUI.Framework;

internal class DiscoBarSeries : BarSeries
{
    private readonly Terminal.Gui.Attribute green;
    private readonly Terminal.Gui.Attribute brightgreen;
    private readonly Terminal.Gui.Attribute brightyellow;
    private readonly Terminal.Gui.Attribute red;
    private readonly Terminal.Gui.Attribute brightred;
    private readonly decimal _multiplier;

    public DiscoBarSeries(decimal multiplier = 1)
    {
        _multiplier = multiplier;
        green = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black);
        brightgreen = Application.Driver.MakeAttribute(Color.Green, Color.Black);
        brightyellow = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black);
        red = Application.Driver.MakeAttribute(Color.Red, Color.Black);
        brightred = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black);
    }

    protected override void DrawBarLine(GraphView graph, Point start, Point end, Bar beingDrawn)
    {
        var driver = Application.Driver;

        var x = start.X;
        for (var y = end.Y; y <= start.Y; y++)
        {
            var height = graph.ScreenToGraphSpace(x, y).Y;

            if ((decimal)height >= _multiplier * .85m)
            {
                driver.SetAttribute(red);
            }
            else if ((decimal)height >= _multiplier * .66m)
            {
                driver.SetAttribute(brightred);
            }
            else if ((decimal)height >= _multiplier * .45m)
            {
                driver.SetAttribute(brightyellow);
            }
            else if ((decimal)height >= _multiplier * .25m)
            {
                driver.SetAttribute(brightgreen);
            }
            else
            {
                driver.SetAttribute(green);
            }

            graph.AddRune(x, y, beingDrawn.Fill.Rune);
        }
    }
}
