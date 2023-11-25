using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
