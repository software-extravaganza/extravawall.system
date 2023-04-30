using ExtravaWallSetup.GUI;

using ExtravaWallSetup.GUI.Framework;

using ExtravaWallSetup.Stages.Framework;

using System.Diagnostics;

using System.Reactive;

using System.Reactive.Linq;

using System.Reactive.Threading.Tasks;

using System.Text.RegularExpressions;

using DynamicData.Kernel;

using NetworkManager.DBus;

using Terminal.Gui;

using Tmds.DBus;

namespace ExtravaWallSetup;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class InstallManager : IDisposable {
    public static InstallManager Instance { get; private set; } = null!;
    public static bool ShouldRun { get; set; }
    public VirtualConsoleManager Console { get; private set; }

    private StageManager _stageManager;

    private readonly IDictionary<string, System.Data.DataRow> _systemInfo = new Dictionary<string, System.Data.DataRow>();

    private MainLoop _mainLoop;

    private readonly ExtravaServiceProvider _serviceProvider;

    public INetworkManager NetworkManager { get; private set; }
    public Connection DbusSystem => Connection.System;
    public event Action OnExiting;
    public event Action OnExited;

    public InstallManager(ExtravaServiceProvider serviceProvider) {
        Instance = this;
        _serviceProvider = serviceProvider;

        NetworkManager = DbusSystem.CreateProxy<INetworkManager>(
            "org.freedesktop.NetworkManager",
            "/org/freedesktop/NetworkManager"
        );
    }

    public View CreateDefaultScreen() {
        DefaultScreen = _serviceProvider.GetService<DefaultScreen>();
        Console = _serviceProvider.GetService<VirtualConsoleManager>();
        _stageManager = _serviceProvider.GetService<StageManager>();
        _ = DefaultScreen?.LayoutInitialized
                    .ToObservable()
                    .Subscribe(async unit => await OnConsoleLayoutInitializedAsync(unit));
        return DefaultScreen ?? new View();
    }

    private async Task OnConsoleLayoutInitializedAsync(Unit unit) {
        _mainLoop = Application.MainLoop;
        _stageManager.Initialize();
        await InitializeAsync();
    }

    private void SystemOnStateChanged(object? sender, ConnectionStateChangedEventArgs args) {
        if (args.State != ConnectionState.Disconnecting) {
            return;
        }

        _ = args.DisconnectReason;
    }

    private readonly TaskCompletionSource _initializationCompletionSource = new();

    public Task InitializedTask => _initializationCompletionSource.Task;

    public async Task InitializeAsync() {
        DbusSystem.StateChanged += SystemOnStateChanged;
        _ = await Connection.System.ConnectAsync();
        _mainLoop?.Invoke(async () => {
            DefaultScreen?.Initialize();
            if (_stageManager.CurrentStage == StageType.Initialize) {
                await _stageManager.AdvanceToStage(StageType.Menu);
            }
        });

        AppDomain.CurrentDomain.ProcessExit += (object? sender, EventArgs args) => { };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) => { };

        AppDomain.CurrentDomain.FirstChanceException += (sender, args) => { };

        _ = _initializationCompletionSource.TrySetResult();
    }

    public async Task InstallAsync() {
        await _stageManager.SkipToStageAndRemoveSkipped(StageType.InstallBegin);
    }

    public async Task RecoverAsync() {
        //todo: implement
        await Task.CompletedTask;
    }

    public void Exit(string? exitContent = null) {
        OnExiting?.Invoke();
        Application.Shutdown();
        var hasError = !string.IsNullOrWhiteSpace(exitContent);
        if (hasError) {
            System.Console.WriteLine(BannerView.ExtravaWall + BannerView.Version + exitContent);
            // if (IsDebug) {
            //     System.Console.WriteLine("\n\nPRESS ENTER");
            //     System.Console.ReadLine();
            // }
        }

        Environment.Exit(hasError ? 1 : 0);
        OnExited?.Invoke();
    }

    public void RequestEndOnNextStep(string reason) {
        _stageManager.RequestEndOnNextStep(reason);
    }

    public DefaultScreen? DefaultScreen { get; private set; }

    public void AddOrUpdateSystemInfo(string property, string value) {
        _mainLoop?.Invoke(() => {
            if (_systemInfo.TryGetValue(property, out var matchRow)) {
                matchRow.ItemArray = new object[] { property, value };
                DefaultScreen.InfoTable.SetNeedsDisplay();
                return;
            }

            var rowData = new List<object>() { property, value };
            var resultRow = DefaultScreen.InfoTable.Table.Rows.Add(rowData.ToArray());
            _systemInfo.Add(property, resultRow);
        });
    }

    public void RemoveSystemInfo(string property) {
        _mainLoop.Invoke(() => _systemInfo.RemoveIfContained(property));
    }

    public void RemoveSystemInfoByPattern(string pattern) {
        RemoveSystemInfoByPattern(new Regex(pattern));
    }

    public void RemoveSystemInfoByPattern(Regex pattern) {
        _mainLoop.Invoke(() => {
            foreach (var info in _systemInfo.ToArray()) {
                var match = pattern.Match(info.Key);
                if (match.Success) {
                    DefaultScreen.InfoTable.Table.Rows.Remove(info.Value);
                    _ = _systemInfo.RemoveIfContained(info.Key);
                }
            }
        });
    }



    void IDisposable.Dispose() {
        DefaultScreen.Dispose();
    }
}
