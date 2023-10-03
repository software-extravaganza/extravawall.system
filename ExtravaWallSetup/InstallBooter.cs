using System.Diagnostics;
using System.Reactive.Concurrency;
using ExtravaCore;
using ExtravaCore.Commands;
using ExtravaCore.Commands.Framework;
using ExtravaWallSetup.GUI.Framework;
using ReactiveUI;

namespace ExtravaWallSetup;

public class InstallBooter {
    private Program _program;
    private ICommandRunner _commandRunner;
    private IElevator _elevator;
    private ExtravaServiceProvider _serviceProvider;

    public InstallBooter(Program program, ICommandRunner commandRunner, IElevator elevator, ExtravaServiceProvider serviceProvider) {
        _program = program;
        _commandRunner = commandRunner;
        _elevator = elevator;
        _serviceProvider = serviceProvider;
    }

    public async Task Run(string[] args) {
        bool _noRoot = false;

        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "--no-root") {
                _noRoot = true;
                break;
            }
        }

        // if (!string.IsNullOrEmpty(tempFile)) {
        //     File.WriteAllText(tempFile, "1");
        // }

        if (HasElevatedPermissions()) {
            _program.Run(this, args);
            return;
        }

        var result = await _commandRunner.For<CommandMachineName>().WithNoInput.RunAsync();
        var result2 = await _commandRunner.For<CommandMachineOs>().WithNoInput.RunAsync();
        var result3 = await _commandRunner.For<CommandMachineArchitecture>().WithNoInput.RunAsync();
        var result4 = await _commandRunner.For<CommandMachineAll>().WithNoInput.RunAsync();
        var result5 = await _commandRunner.For2<CommandPackagesInstalled>().(o => o.Package = "neofetch").RunAsync();
        var result6 = await _commandRunner.For<CommandPackagesInstalled>().Options(o => o.Package = "tmux").RunAsync();
        var result7 = await _commandRunner.For<CommandPackagesInstalled>().RunAsync();

        Console.WriteLine(result.Result);
        // var elevator = new Elevator();
        // elevator.RestartAndRunElevated(() => {
        //     _program.Run(this, args);
        // });

        RxApp.MainThreadScheduler = TerminalScheduler.Default;
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;

        RestartAndRunElevated(_noRoot, () => _program.Run(this, args));
    }


    private static bool HasElevatedPermissions() {
        return (int)Interop.geteuid() == 0;
    }

    public void RestartAndRunElevated(bool noRoot, Action action) {
        if (!noRoot && !HasElevatedPermissions()) {
            //var writer = Console.GetNewWriter(Color.Gray);
            System.Console.WriteLine("This application requires elevated permissions to run.");
            System.Console.WriteLine("Please enter the root password to continue.");

            _elevator.RestartAndRunElevated(() => { });
            return;
        }
        // //Can not open file /home/phil/src/extrava/extravawall/ExtravaWallSetup/bin/Debug/net7.0/localhost:3000
        // //string debuggerCommand = $"/opt/JetBrains/Rider-2021.3.1/bin/rider.sh --line:{Debugger.IsAttached ? Debugger.CurrentSourceLine : 12} --no-splash --pid {pid}"
        // int pid = Process.GetCurrentProcess().Id;
        //
        // //string debuggerCommand = $"/snap/bin/rider --no-splash --attach {pid}";
        // int port = 8888; // Change this to a port that is not in use on your system.
        // string debuggerCommand = $"/snap/bin/rider --line:12 --no-splash --open=socket://127.0.0.1:{port}";
        // var debuggerProcess = Process.Start("bash", $"-c \"{debuggerCommand}\"");
        //
        // // Wait for the debugger server to start.
        // WaitForDebugger(port);
        //
        // // Connect to the debugger server.
        // var socket = new TcpClient();
        // socket.Connect(new IPEndPoint(IPAddress.Loopback, GetDebuggerPort()));
        //
        // // Send a message to attach the debugger to the current process.
        // var stream = socket.GetStream();
        // stream.WriteByte(0x01);

        action();
    }
}

