using ExtravaWallSetup.Commands;
using ExtravaWallSetup.Commands.Framework;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static Terminal.Gui.View;

namespace ExtravaWallSetup.GUI.Framework {
    public class VirtualConsoleManager {
        private InstallManager _installManager;
        private DefaultScreen _defaultScreen;
        private ScrollView _consoleView;
        private int _totalScrollWidth;
        private int _totalScrollHeight = 2;

        public VirtualConsoleManager(InstallManager installManager, DefaultScreen defaultScreen, ScrollView consoleView) {
            _installManager = installManager;
            _defaultScreen = defaultScreen;
            _consoleView = consoleView;
            Application.Resized = defaultScreen_Resized;
            _defaultScreen.KeyPress += defaultScreen_KeyDown;
        }

        public ITextOutput GetNewWriter(Color foreground = Color.Green, Color background = Color.Black) {
            var textView = new DisplayTextView(foreground, background);
            Add(textView);
            return textView;
        }

        public async Task CommandAsync<T>(Action<T, CommandView> actions, bool printResult = false) where T : ICommand, new() {
            await Task.Run(async () => {
                var commandView = Add(new CommandView());
                var command = new T();
                command.SetOutput(printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None);
                command.SetCommandView(commandView);
                actions.Invoke(command, commandView);
            }).ConfigureAwait(false);
        }

        public async Task CommandAsync<T>(Func<T, CommandView, Task> actions, bool printResult = false) where T : ICommand, new() {
            await Task.Run(async () => {
                var commandView = Add(new CommandView());
                var command = new T();
                command.SetOutput(printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None);
                command.SetCommandView(commandView);
                await actions.Invoke(command, commandView);
            }).ConfigureAwait(false);
        }

        //public async Task<TReturn> CommandAsync<T, TReturn>(Func<T, CommandView, Task<TReturn>> actions, bool printResult = false) where T : ICommand, new() {
        //    await Task.Run(async () => {
        //        var commandView = Add(new CommandView());
        //        var command = new T();
        //        command.SetOutput(printResult ? CommandOutputType.VirtualConsole : CommandOutputType.None);
        //        command.SetCommandView(commandView);
        //        var result = await actions.Invoke(command, commandView);
        //        return result;
        //    }).ConfigureAwait(false);
        //}
        private IDictionary<View, Size> _consoleChildrenSizes = new Dictionary<View, Size>();
        private IDictionary<View, Point> _consoleChildrenPositions = new Dictionary<View, Point>();
        public T Add<T>(T view) where T : View {
            Application.MainLoop.Invoke(() => {
                var lowestPosition = _consoleView.Subviews.First().Subviews.LastOrDefault();
                int? calculatedBottom = null;
                if (lowestPosition != null && _consoleChildrenSizes.ContainsKey(lowestPosition) && _consoleChildrenPositions.ContainsKey(lowestPosition)) {
                    calculatedBottom = _consoleChildrenSizes[lowestPosition].Height + _consoleChildrenPositions[lowestPosition].Y;
                }
                _consoleView.Add(view);
                view.GetCurrentWidth(out int currentWidth);
                view.GetCurrentHeight(out int currentHeight);
                var size = new Size(currentWidth, currentHeight);
                var widthIsFill = view.Width == Dim.Fill(0);
                var heightIsFill = view.Height == Dim.Fill(0);
                if (widthIsFill) {
                    _consoleView.GetCurrentWidth(out int scrollViewWidth);
                    size.Width = scrollViewWidth - 1;
                    view.Width = size.Width;
                }

                if (heightIsFill) {
                    _consoleView.GetCurrentHeight(out int scrollViewHeight);
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
                _consoleView.SetFocus();
                RefreshConsole();
            });

            return view;
        }

        private void resetConsoleScroll(View viewRefresh = null, Size? size = null) {
            Application.MainLoop.Invoke(() => {
                var widthDiff = 0;
                var heightDiff = 0;
                if (viewRefresh != null) {
                    int currentWidth = 0;
                    int currentHeight = 0;
                    if (size is not null) {
                        currentWidth = size.Value.Width;
                        currentHeight = size.Value.Height;
                    }
                    else {
                        viewRefresh.GetCurrentWidth(out currentWidth);
                        viewRefresh.GetCurrentHeight(out currentHeight);
                    }

                    if (_consoleChildrenSizes.ContainsKey(viewRefresh)) {
                        var storedSize = _consoleChildrenSizes[viewRefresh];
                        widthDiff = currentWidth - storedSize.Width;
                        heightDiff = currentHeight - storedSize.Height;
                        _consoleChildrenSizes[viewRefresh] = new Size(currentWidth, currentHeight);
                    }
                    _totalScrollHeight += heightDiff;
                }
                _consoleView.GetCurrentWidth(out int scrollViewWidth);
                _consoleView.GetCurrentHeight(out int scrollViewHeight);
                _consoleView.ContentSize = new Size(_totalScrollWidth, _totalScrollHeight);
                _consoleView.ContentOffset = new Point(0, _consoleView.ContentSize.Height);
                _consoleView.SetNeedsDisplay();
            });
        }

        private void defaultScreen_Resized(Application.ResizedEventArgs obj) {
            resetConsoleScroll();
        }
        private void defaultScreen_KeyDown(KeyEventEventArgs obj) {
            if (obj.KeyEvent.Key == Key.F5) {
                RefreshConsole();
            }
        }

        public void RefreshConsole(View viewRefresh = null, Size? size = null) {
            Application.MainLoop.Invoke(() => {
                _defaultScreen.Redraw(_defaultScreen.Bounds);
            });
            resetConsoleScroll(viewRefresh, size);
        }
    }
}
