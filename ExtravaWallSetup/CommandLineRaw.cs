using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExtravaWallSetup;

public static class CommandLineRaw {
#if WINDOWS
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetCommandLine();
#endif

    public static string GetFullCommandLine() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#if WINDOWS
            return Marshal.PtrToStringAuto(GetCommandLine());
#else
            return string.Empty;
#endif
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            var processId = Environment.ProcessId.ToString();
            return File.ReadAllText($"/proc/{processId}/cmdline").Replace("\0", " ").Trim();
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            var processId = Environment.ProcessId.ToString();
            var psi = new ProcessStartInfo("ps", $"-p {processId} -o command= ") {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var process = Process.Start(psi);
            return process?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
        } else {
            throw new NotSupportedException("This platform is not supported.");
        }
    }
}
