using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Claunia.PropertyList;

namespace WeblocOpener;

internal static class Program
{
    private const string ProgId = "WeblocOpener.webloc";
    private const string Ext = ".webloc";
    private const string FriendlyTypeName = "macOS Webloc Shortcut";
    private const string AppName = "webloc-opener";

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return ShowUsage();

            var arg0 = args[0].Trim();

            if (IsSwitch(arg0, "--register", "/register", "-register"))
            {
                Register();
                ShowInfo("Registered.\n\nTo set as default for .webloc files:\nRight-click a .webloc → Open with → Choose another app → Always.");
                return 0;
            }

            if (IsSwitch(arg0, "--unregister", "/unregister", "-unregister"))
            {
                Unregister();
                ShowInfo("Unregistered.");
                return 0;
            }

            if (IsSwitch(arg0, "--help", "/help", "-h", "/?"))
                return ShowUsage();

            var path = arg0.Trim('"');
            if (!File.Exists(path))
            {
                ShowError($"File not found: {path}");
                return 2;
            }

            var url = ExtractUrlFromWebloc(path);
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowError("Could not find URL in .webloc file.");
                return 3;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return 0;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            return 1;
        }
    }

    private static bool IsSwitch(string value, params string[] switches)
    {
        foreach (var s in switches)
        {
            if (string.Equals(value, s, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static int ShowUsage()
    {
        ShowInfo(
            "webloc-opener\n\n" +
            "Usage:\n" +
            "  webloc-opener.exe <file.webloc>     Opens the URL in default browser\n" +
            "  webloc-opener.exe --register        Register handler for .webloc\n" +
            "  webloc-opener.exe --unregister      Remove handler");
        return 64;
    }

    private static string? ExtractUrlFromWebloc(string path)
    {
        var fileInfo = new FileInfo(path);
        var root = PropertyListParser.Parse(fileInfo);

        if (root is NSDictionary dict)
        {
            var obj = dict.ObjectForKey("URL");
            return obj?.ToString();
        }

        if (root is NSArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is NSDictionary d)
                {
                    var obj = d.ObjectForKey("URL");
                    if (obj != null) return obj.ToString();
                }
            }
        }

        return null;
    }

    private static string GetExecutablePath()
    {
        var path = Environment.ProcessPath
                   ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("Cannot determine executable path.");
        return path;
    }

    private static void Register()
    {
        var exePath = GetExecutablePath();

        using var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes")!;

        using (var extKey = classes.CreateSubKey(Ext)!)
        {
            extKey.SetValue("", ProgId);
            extKey.SetValue("Content Type", "application/x-webloc");
        }

        using (var progKey = classes.CreateSubKey(ProgId)!)
        {
            progKey.SetValue("", FriendlyTypeName);
            using (var iconKey = progKey.CreateSubKey("DefaultIcon")!)
                iconKey.SetValue("", $"\"{exePath}\",0");
            using (var cmdKey = progKey.CreateSubKey(@"shell\open\command")!)
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
        }

        using (var openWith = Registry.CurrentUser.CreateSubKey(
                   $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{Ext}\OpenWithProgids"))
        {
            openWith?.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }

        NotifyShell();
    }

    private static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Ext}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);

        using (var openWith = Registry.CurrentUser.OpenSubKey(
                   $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{Ext}\OpenWithProgids",
                   writable: true))
        {
            openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
        }

        NotifyShell();
    }

    private static void ShowInfo(string message)
        => MessageBoxW(IntPtr.Zero, message, AppName, MB_OK | MB_ICONINFORMATION);

    private static void ShowError(string message)
        => MessageBoxW(IntPtr.Zero, message, AppName, MB_OK | MB_ICONERROR);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONINFORMATION = 0x00000040;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    private static void NotifyShell()
        => SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
