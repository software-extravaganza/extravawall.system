using System.Diagnostics;

namespace ExtravaCore;

public interface IProcessManager : IDisposable {
    Thread CreateAndStartThread(ThreadStart threadStart);
    string GetCurrentExecutionLocation();
    int GetCurrentProcessId();
    int GetParentProcessId(int processId);
    ThreadStart GetThreadStartFor(ProcessStartInfo startInfo, Action? postDelegate = null, CancellationToken cancellationToken = default);
    string ReadProcessCommandline(int processId);
}

public class ProcessManager : IProcessManager {
    private List<WeakReference<Thread>> _threads = new();
    private List<WeakReference<Process>> _processes = new();

    public int GetParentProcessId(int processId) {
        // Open the /proc/[pid]/stat file for the current process
        using FileStream fs = File.OpenRead($"/proc/{processId}/stat");
        using StreamReader reader = new(fs);
        // Read the process ID, command name, and parent process ID from the file
        string line = reader.ReadLine()!;
        string[] fields = line.Split(' ');
        int parentProcessId = int.Parse(fields[3]);
        return parentProcessId;
    }
    public string ReadProcessCommandline(int processId) {
        // Read the command line used to launch the process from the /proc/[pid]/cmdline file
        string cmdLine = "";
        using (StreamReader reader = new StreamReader($"/proc/{processId}/cmdline")) {
            cmdLine = reader.ReadToEnd();
        }

        // Replace null characters with spaces
        cmdLine = cmdLine.Replace('\0', ' ');

        // Trim whitespace from the beginning and end of the command line
        cmdLine = cmdLine.Trim();

        return cmdLine;
    }

    public ThreadStart GetThreadStartFor(ProcessStartInfo startInfo, Action? postDelegate = null, CancellationToken cancellationToken = default) {
        ThreadStart threadStart = () => {
            Console.WriteLine("Starting Elevated...");
            var process = new Process() {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            try {
                process.Start();
                _processes.Add(new WeakReference<Process>(process));
                cancellationToken.Register(() => process?.Close());
                process?.WaitForExit();
                var exitCode = process?.ExitCode ?? 0;
                Console.WriteLine($"Process exited with code: {exitCode}");
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
            postDelegate?.Invoke();
        };
        return threadStart;
    }

    public Thread CreateAndStartThread(ThreadStart threadStart) {
        // Create and start a new thread to run the delegate
        Thread elevatedProcessThread = new Thread(threadStart);
        elevatedProcessThread.Start();
        _threads.Add(new WeakReference<Thread>(elevatedProcessThread));
        return elevatedProcessThread;
    }

    public int GetCurrentProcessId() => Environment.ProcessId;
    public string GetCurrentExecutionLocation() => System.Reflection.Assembly.GetExecutingAssembly().Location;

    public void Dispose() {
        foreach (var thread in _threads) {
            try {
                if (thread.TryGetTarget(out var target) && target?.IsAlive == true) {
                    target?.Interrupt();
                }
            } catch (ThreadInterruptedException) {

            } finally {
                thread.SetTarget(null!);
            }
        }

        foreach (var process in _processes) {
            try {
                if (process.TryGetTarget(out var target) && target?.HasExited == false) {
                    target?.Close();
                }
            } catch (InvalidOperationException) {

            } finally {
                process.SetTarget(null!);
            }
        }

        _threads.RemoveAll(t => !t.TryGetTarget(out var target) || target == null);
        _processes.RemoveAll(p => !p.TryGetTarget(out var target) || target == null);
    }
}