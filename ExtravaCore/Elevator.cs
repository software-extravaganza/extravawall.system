using System.Diagnostics;
using System.Text;

namespace ExtravaCore;

public interface IElevator : IDisposable {
    ProcessStartInfo GetElevatedProcessStartInfo(bool exactParentCommand = false);
    void RestartAndRunElevated(Action? exitDelegate = null);
}

public partial class Elevator : IElevator {
    private bool isDisposed;
    private readonly IProcessManager _processManager;
    public Elevator(IProcessManager processManager) {
        _processManager = processManager;
    }

    public void Dispose() {
        _processManager.Dispose();
        isDisposed = true;
    }

    public ProcessStartInfo GetElevatedProcessStartInfo(bool exactParentCommand = false) {
        throwIfDisposed();
        var commandBuilder = new StringBuilder();
        if (exactParentCommand) {
            // Get the process ID of the current process
            int currentProcessId = _processManager.GetCurrentProcessId();

            // Get the process ID of the parent process
            int parentProcessId = _processManager.GetParentProcessId(currentProcessId);

            // Read the command line used to launch the parent process from the /proc filesystem
            commandBuilder.Append(_processManager.ReadProcessCommandline(parentProcessId));
        } else {
            // Get information about the current process
            string currentExePath = _processManager.GetCurrentExecutionLocation();
            commandBuilder.Append(currentExePath.ToLower().EndsWith(".dll") ? "dotnet " : string.Empty);
            commandBuilder.Append(currentExePath);
        }

        // Create a ProcessStartInfo object with the necessary settings
        var startInfo = new ProcessStartInfo {
            FileName = "sudo",
            Arguments = commandBuilder.ToString(),
            UseShellExecute = false
        };

        return startInfo;
    }

    public void RestartAndRunElevated(Action? exitDelegate = null) {
        throwIfDisposed();
        Console.WriteLine("Executing with elevated permissions...");
        var startElevatedProcess = _processManager.GetThreadStartFor(GetElevatedProcessStartInfo(), () => {
            exitDelegate?.Invoke();
        });

        _processManager.CreateAndStartThread(startElevatedProcess);
    }

    private void throwIfDisposed() {
        if (isDisposed) {
            throw new ObjectDisposedException(nameof(Elevator));
        }
    }
}