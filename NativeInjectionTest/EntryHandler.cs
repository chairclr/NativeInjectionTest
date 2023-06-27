using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NativeInjectionTest;

public partial class EntryHandler
{
    // Called when we inject, hopefully
    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) }, EntryPoint = "NativeEntry")]
    public static void NativeEntry()
    {
        Thread thread = new Thread(Load);

        thread.Start();
    }

    private static void Load()
    {
        // Do actual loading and whatnot code in here
        // Due to the fact that the injector waits for the NativeEntry thread to exit,
        // The thread that NativeEntry is called in should exit as soon as possible

        nint hWnd = Process.GetCurrentProcess().MainWindowHandle;

        Assembly assembly = Assembly.GetExecutingAssembly();

        string message =
            $"""
            Load() has been called

            Process Id: {Environment.ProcessId}
            HWND (for MessageBox): {hWnd}
            Thread Id: {Environment.CurrentManagedThreadId}
            Architecture: {(Environment.Is64BitProcess ? "x86_64" : "x86")}
            Assembly Name: {assembly.GetName()}
            Base Directory: {AppContext.BaseDirectory}
            Stack Trace:
            {new StackTrace(true)}
            """;

        Console.WriteLine(message);
        Debug.WriteLine(message);

        MessageBox(hWnd, message, "Hello from NativeAOT injection", 0);
        Environment.Exit(0);
    }

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(nint hWnd, string text, string caption, uint type);
}
