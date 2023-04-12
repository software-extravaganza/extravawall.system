using ExtravaWallSetup.Commands.Framework;
using Terminal.Gui;
using static Terminal.Gui.View;

namespace ExtravaWallSetup.GUI.Framework;

[System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ExtravaScrollView : ScrollView
{
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => ToString();

    private ExtravaScrollView? findScrollView(View parent)
    {
        if (parent is ExtravaScrollView scrollView)
        {
            return scrollView;
        }
        else if (parent.SuperView != null)
        {
            return findScrollView(parent.SuperView);
        }

        return null;
    }

    public override bool ProcessHotKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.CursorUp)
        {
            _ = (findScrollView(this)?.ScrollUp(1));
        }
        else if (keyEvent.Key == Key.CursorDown)
        {
            _ = (findScrollView(this)?.ScrollDown(1));
        }
        else if (keyEvent.Key == Key.CursorRight)
        {
            _ = (findScrollView(this)?.ScrollRight(1));
        }
        else if (keyEvent.Key == Key.CursorLeft)
        {
            _ = (findScrollView(this)?.ScrollLeft(1));
        }

        return base.ProcessHotKey(keyEvent);
    }
}

[System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
public class VirtualConsoleManager
{
    private readonly DefaultScreen _defaultScreen;
    private readonly ScrollView _consoleView;
    private int _totalScrollWidth;
    private int _totalScrollHeight = 2;

    public VirtualConsoleManager(DefaultScreen defaultScreen, ExtravaScrollView consoleView)
    {
        _defaultScreen = defaultScreen;
        _consoleView = consoleView;
        _consoleView.KeepContentAlwaysInViewport = true;
        Application.Resized = defaultScreen_Resized;
        _defaultScreen.KeyDown += defaultScreen_KeyDown;
    }

    public ITextOutput GetNewWriter(Color foreground = Color.Green, Color background = Color.Black)
    {
        var textView = new DisplayTextView(foreground, background);
        _ = Add(textView);
        return textView;
    }

    public IProgressBar GetNewProgressBar(float initialProgress, string text = "")
    {
        var progressView = new DisplayProgressBar(initialProgress, text);
        _ = Add(progressView);
        return progressView;
    }

    public async Task CommandAsync<T>(Action<T, CommandView> actions, bool printResult = false)
        where T : ICommand, new()
    {
        await Task.Run(() =>
        {
            var commandView = new CommandView();
            var command = new T();
            if (printResult)
            {
                _ = Add(commandView);
            }

            command.SetOutput(
                printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None
                );
            command.SetCommandView(commandView);
            actions.Invoke(command, commandView);
            return Task.CompletedTask;
        })
            .ConfigureAwait(false);
    }

    public async Task CommandAsync<T>(Func<T, CommandView, Task> actions, bool printResult = false)
        where T : ICommand, new()
    {
        await Task.Run(async () =>
        {
            var commandView = new CommandView();
            var command = new T();

            if (printResult)
            {
                _ = Add(commandView);
            }

            command.SetOutput(
                printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None
                );
            command.SetCommandView(commandView);
            await actions.Invoke(command, commandView);
        })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// public async Task<TReturn> CommandAsync<T, TReturn>(Func<T, CommandView, Task<TReturn>> actions, bool printResult = false) where T : ICommand, new() {
    /// await Task.Run(async () => {
    /// var commandView = Add(new CommandView());
    /// var command = new T();
    /// command.SetOutput(printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None);
    /// command.SetCommandView(commandView);
    /// var result = await actions.Invoke(command, commandView);
    /// return result;
    /// }).ConfigureAwait(false);
    /// }
    /// </summary>
    private readonly IDictionary<View, Size> _consoleChildrenSizes = new Dictionary<View, Size>();
    private readonly IDictionary<View, Point> _consoleChildrenPositions =
        new Dictionary<View, Point>();

    public T Add<T>(T view)
        where T : View
    {
        Application.MainLoop.Invoke(() =>
        {
            lock (_lockScrollViewRefresh)
            {
                var lowestPosition = _consoleView.Subviews[0].Subviews.LastOrDefault();
                int? calculatedBottom = null;
                if (lowestPosition != null
                        && _consoleChildrenSizes.ContainsKey(lowestPosition)
                        && _consoleChildrenPositions.ContainsKey(lowestPosition)
                    )
                {
                    calculatedBottom =
                        _consoleChildrenSizes[lowestPosition].Height
                            + _consoleChildrenPositions[lowestPosition].Y;
                }

                _consoleView.Add(view);
                _ = view.GetCurrentWidth(out var currentWidth);
                _ = view.GetCurrentHeight(out var currentHeight);
                var size = new Size(currentWidth, currentHeight);
                var widthIsFill = view.Width == Dim.Fill(0);
                var heightIsFill = view.Height == Dim.Fill(0);
                if (widthIsFill)
                {
                    _ = _consoleView.GetCurrentWidth(out var scrollViewWidth);
                    size.Width = scrollViewWidth - 1;
                    view.Width = size.Width;
                }

                if (heightIsFill)
                {
                    _ = _consoleView.GetCurrentHeight(out var scrollViewHeight);
                    size.Height = scrollViewHeight - 1;
                    view.Height = size.Height;
                }

                var position = new Point(0, calculatedBottom ?? 0);
                _consoleChildrenSizes.Add(view, size);
                _consoleChildrenPositions.Add(view, position);
                view.X = position.X;
                view.Y = position.Y;
                //view.Y = lowestPosition == null ? Pos.At(0) : Pos.Bottom(lowestPosition) + 1;
                //view.Y = ;
                //view.X = Pos.At(0);
                _totalScrollWidth += currentWidth;
                _totalScrollHeight += currentHeight;
                //_consoleView.SetFocus();

                RefreshConsole();
            }
        });

        return view;
    }

    private readonly object _lockScrollViewRefresh = new();

    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => ToString();

    private void resetConsoleScroll(View? viewRefresh = null, Size? size = null)
    {
        Application.MainLoop.Invoke(() =>
        {
            lock (_lockScrollViewRefresh)
            {
                var widthDiff = 0;
                var heightDiff = 0;
                if (viewRefresh != null)
                {
                    var currentWidth = 0;
                    var currentHeight = 0;
                    if (size is not null)
                    {
                        currentWidth = size.Value.Width;
                        currentHeight = size.Value.Height;
                    }
                    else
                    {
                        _ = viewRefresh.GetCurrentWidth(out currentWidth);
                        _ = viewRefresh.GetCurrentHeight(out currentHeight);
                    }

                    if (_consoleChildrenSizes.ContainsKey(viewRefresh))
                    {
                        var storedSize = _consoleChildrenSizes[viewRefresh];
                        widthDiff = currentWidth - storedSize.Width;
                        heightDiff = currentHeight - storedSize.Height;
                        _consoleChildrenSizes[viewRefresh] = new Size(currentWidth, currentHeight);
                    }

                    _totalScrollHeight += heightDiff;
                }

                //_totalScrollHeight = _consoleChildrenSizes.Sum(v => v.Value.Height);
                _ = _consoleView.GetCurrentWidth(out var scrollViewWidth);
                _ = _consoleView.GetCurrentHeight(out var scrollViewHeight);
                _consoleView.ContentSize = new Size(_totalScrollWidth, _totalScrollHeight);
                _consoleView.ContentOffset = new Point(0, _totalScrollHeight);
                _consoleView.ContentOffset = new Point(0, 0);
                _ = _consoleView.ScrollDown(_totalScrollHeight);
                //_consoleView.Subviews.FirstOrDefault(v => v.GetType() == typeof(ScrollBarView))?.
                // _consoleView.ScrollDown(heightDiff+ scrollViewHeight);
                _consoleView.SetNeedsDisplay();
            }
        });
    }

    private void defaultScreen_Resized(Application.ResizedEventArgs obj)
    {
        resetConsoleScroll();
    }

    private void defaultScreen_KeyDown(KeyEventEventArgs obj)
    {
        if (obj.KeyEvent.Key != Key.F5)
        {
            return;
        }

        RefreshConsole();
    }

    public void RefreshConsole(View? viewRefresh = null, Size? size = null)
    {
        resetConsoleScroll(viewRefresh, size);

        Application.MainLoop.Invoke(RefreshConsoleAction);
    }

    private void RefreshConsoleAction()
    {
        _defaultScreen.Redraw(_defaultScreen.Bounds);
        _consoleView.SetNeedsDisplay();
    }
}
