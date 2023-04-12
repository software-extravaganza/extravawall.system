using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ExtravaWallSetup.GUI;
using ExtravaWallSetup.GUI.Framework;
using Terminal.Gui;

internal static class Interop {
    [DllImport("libc")]
    public static extern uint geteuid();

    [DllImport("libc")]
    public static extern int setuid(uint uid);



    public static void sudo(string[] args) {
        var startInfo = new ProcessStartInfo {
            FileName = "sudo",
            Arguments = $"dotnet {string.Join(' ', args)}",
            UseShellExecute = false
        };
        Process.Start(startInfo)?.WaitForExit();
    }

}

public class Elevator{
    public static bool IsDebug {
        get {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
static int GetParentProcessId(int processId)
    {
        // Open the /proc/[pid]/stat file for the current process
        using (FileStream fs = File.OpenRead($"/proc/{processId}/stat"))
        using (StreamReader reader = new StreamReader(fs))
        {
            // Read the process ID, command name, and parent process ID from the file
            string line = reader.ReadLine();
            string[] fields = line.Split(' ');
            int parentProcessId = int.Parse(fields[3]);
            return parentProcessId;
        }
    }
    static string ReadProcCmdline(int processId)
    {
        // Read the command line used to launch the process from the /proc/[pid]/cmdline file
        string cmdLine = "";
        using (StreamReader reader = new StreamReader($"/proc/{processId}/cmdline"))
        {
            cmdLine = reader.ReadToEnd();
        }

        // Replace null characters with spaces
        cmdLine = cmdLine.Replace('\0', ' ');

        // Trim whitespace from the beginning and end of the command line
        cmdLine = cmdLine.Trim();

        return cmdLine;
    }
    
    public void RestartAndRunElevated(Action exitDelegate = null) {
            
        System.Console.WriteLine("Executing with elevated permissions...");
            Process currentProcess = Process.GetCurrentProcess();
            // Get the process ID of the current process
            int currentProcessId = Process.GetCurrentProcess().Id;

            // Get the process ID of the parent process
            int parentProcessId = GetParentProcessId(currentProcessId);

            // Read the command line used to launch the parent process from the /proc filesystem
            string commandLine = ReadProcCmdline(parentProcessId);

            // Get the command line used to launch the current process
            //string commandLine = currentProcess.StartInfo.Arguments;
            
            // Define a delegate to start the new process with 'pkexec' and shut down the Terminal.Gui application
            ThreadStart startElevatedProcess = () => {
                string tempFilePath = Path.Combine("/var/tmp", Path.GetRandomFileName());
                
                // Get information about the current process
                string currentExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var commandBuilder = new StringBuilder();
                //string debuggerCommand = $"/snap/bin/rider --line:93 --no-splash --debug localhost:3000 "; //{Debugger.IsAttached ? Debugger.CurrentSourceLine : 93}
                //Process.Start("bash", $"-c \"{debuggerCommand}\"");
                //commandBuilder.Append(IsDebug ? debuggerCommand : string.Empty);
                commandBuilder.Append(currentExePath.ToLower().EndsWith(".dll") ? "dotnet " : string.Empty);
                
                commandBuilder.Append(currentExePath);
                string ifDebugFlag = IsDebug ? " --debug" : string.Empty;
                // Create a ProcessStartInfo object with the necessary settings
                
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = commandBuilder.ToString(),
                    UseShellExecute = false
                };
                // Process.Start(startInfo)?.WaitForExit();
                //
                // ProcessStartInfo psi = new ProcessStartInfo {
                //     FileName = "/bin/sh",
                //     Arguments = $"-c \"clear; echo Starting with elevated privilages...; sudo {commandLine}{ifDebugFlag}\"",
                //     UseShellExecute = true,
                //     CreateNoWindow = true,
                //     RedirectStandardOutput = false,
                //     RedirectStandardError = false
                // };
                
                
                // Start the new process using the ProcessStartInfo object
                //Process process = new Process { StartInfo = startInfo };
                    System.Console.WriteLine("Starting Elevated...");
                    Process.Start(startInfo)?.WaitForExit();

                    
                    // process.OutputDataReceived += (sender, e) =>
                    // {
                    //     if (!string.IsNullOrEmpty(e.Data))
                    //     {
                    //         writer.WriteLine(e.Data);
                    //     }
                    // };
                    //
                    // var stderrBuilder = new StringBuilder();
                    // process.ErrorDataReceived += (sender, e) =>
                    // {
                    //     if (!string.IsNullOrEmpty(e.Data)) {
                    //         stderrBuilder.Append(e.Data);
                    //     }
                    // };
                    // process.Exited += (sender, args) => {
                    //     System.Console.WriteLine("Exiting");
                    //     Environment.Exit(0);
                    // };
                    //
                    // AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs args) => {
                    //     //psi.EnvironmentVariables["DISPLAY"] = "";
                    //     process.Start();
                    //     Environment.Exit(0);
                    // };
                    //
                    //
                    //
                    // _mainLoop.Invoke(() => {
                    //     Terminal.Gui.Application.Shutdown();
                    // });
                   
                    // process.BeginOutputReadLine();
                    // process.BeginErrorReadLine();
                    //
                    // // Wait for a short period to see if the process exits immediately
                    // if (process.WaitForExit(5000)) {
                    //     process.CancelOutputRead();
                    //     process.CancelErrorRead();
                    //
                    //     var stderr = stderrBuilder.ToString();
                    //     if (!string.IsNullOrEmpty(stderr)) {
                    //         
                    //         _instance.RequestEndOnNextStep(stderr);
                    //     }
                    //     else {
                    //         _instance.RequestEndOnNextStep("Application didn't restart.");
                    //     }
                    // }
                    //
                    // else {
                    //     process.CancelOutputRead();
                    //     process.CancelErrorRead();
                    //     
                    //     writer.WriteLine("Checking for running replacement");
                    //     // Wait for the temporary file to be created
                    //     while (!File.Exists(tempFilePath)) {
                    //         Thread.Sleep(100);
                    //     }
                    //
                    //     // Delete the temporary file
                    //     File.Delete(tempFilePath);
                    //     writer.WriteLine("Exiting");
                    //     Application.MainLoop.Invoke(() => { Exit(); });
                    // }
                    
                    exitDelegate?.Invoke();
                

                
            };

            // Create and start a new thread to run the delegate
            Thread elevatedProcessThread = new Thread(startElevatedProcess);
            elevatedProcessThread.Start();
            
    }
}