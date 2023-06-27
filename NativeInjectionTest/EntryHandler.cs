using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NativeInjectionTest;

public class EntryHandler
{
    // Called when we inject, hopefully
    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) }, EntryPoint = "NativeEntry")]
    public static void NativeEntry()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        string message =
            $"""
            NativeEntry() has been called

            Process Id: {Environment.ProcessId}
            Thread Id: {Environment.CurrentManagedThreadId}
            Architecture: {(Environment.Is64BitProcess ? "x86_64" : "x86")}
            Assembly Name: {assembly.GetName()}
            Base Directory: {AppContext.BaseDirectory}
            Stack Trace:
            {new StackTrace(true)}
            """;

        Debug.WriteLine(message);
        Console.WriteLine(message);

        Thread thread = new Thread(Load);

        thread.Start();
    }

    private static void Load()
    {
        // Do actual loading and whatnot code in here
        // Due to the fact that the injector waits for the NativeEntry thread to exit,
        // The thread that NativeEntry is called in should exit as soon as possible
    }
}
