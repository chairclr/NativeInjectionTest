using System.Diagnostics;
using System.Text;

namespace NativeInjector;

internal class Injector
{
    public static void InjectIntoProcess(Process process, string filePath, string entryPoint)
    {
        if (!File.Exists(filePath))
        {
            filePath = Path.GetFullPath(filePath);
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Invalid file path", filePath);
        }

        if (ProcessContainsModule(process, filePath))
        {
            throw new InvalidOperationException($"Process already contains module '{filePath}'");
        }

        int openProcFlags = NativeMethods.PROCESS_CREATE_THREAD |
            NativeMethods.PROCESS_QUERY_INFORMATION |
            NativeMethods.PROCESS_VM_OPERATION |
            NativeMethods.PROCESS_VM_WRITE |
            NativeMethods.PROCESS_VM_READ;

        nint openProcHandle = NativeMethods.OpenProcess(openProcFlags, false, process!.Id);

        if (openProcHandle == 0)
        {
            throw new Exception($"Failed to obtain handle to process '{process}'");
        }

        try
        {
            LoadLibrary(openProcHandle, filePath);

            // We must refresh the process since we've loaded a new module
            process.Refresh();

            ProcessModule? module = null;

            for (int i = 0; i < process.Modules.Count; i++)
            {
                if (process.Modules[i].FileName == filePath)
                {
                    module = process.Modules[i];
                }
            }

            if (module is null)
            {
                throw new Exception("Failed to get module after injecting");
            }

            CallExport(openProcHandle, process, module, entryPoint);
        }
        finally
        {
            NativeMethods.CloseHandle(openProcHandle);
        }
    }

    private static void LoadLibrary(nint openProcHandle, string filePath)
    {
        uint length = (uint)filePath.Length + 1;

        nint loadLibraryAddr = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandleA("kernel32.dll"), "LoadLibraryA");

        if (loadLibraryAddr == 0)
        {
            throw new Exception($"Failed to obtain function pointer to LoadLibraryA");
        }

        nint loadLibraryMemAddr = NativeMethods.VirtualAllocEx(openProcHandle, IntPtr.Zero, length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);

        if (loadLibraryMemAddr == 0)
        {
            throw new Exception($"Failed to allocate memory for filePath");
        }

        if (!NativeMethods.WriteProcessMemory(openProcHandle, loadLibraryMemAddr, Encoding.UTF8.GetBytes(filePath), length, out _))
        {
            throw new Exception($"Failed to write filePath to remote memory");
        }

        nint loadLibraryThread = NativeMethods.CreateRemoteThread(openProcHandle, IntPtr.Zero, 0, loadLibraryAddr, loadLibraryMemAddr, 0, IntPtr.Zero);

        if (loadLibraryThread == 0)
        {
            throw new Exception($"Failed to create LoadLibraryA thread in remote process");
        }

        try
        {
            int loadLibraryWaitResult = NativeMethods.WaitForSingleObject(loadLibraryThread);

            if (loadLibraryWaitResult != 0)
            {
                throw new Exception($"LoadLibaryA thread failed: {loadLibraryWaitResult:X}");
            }
        }
        finally
        {
            NativeMethods.CloseHandle(loadLibraryThread);
        }
    }

    private static void CallExport(nint openProcHandle, Process process, ProcessModule module, string entryPoint)
    {
        nint entryAddr = NativeMethods.GetRemoteProcAddress(process, module, entryPoint);

        if (entryAddr == 0)
        {
            throw new Exception($"Failed to get address of remote export '{entryPoint}'");
        }

        nint entryThread = NativeMethods.CreateRemoteThread(openProcHandle, IntPtr.Zero, 0, entryAddr, 0, 0, IntPtr.Zero);

        if (entryThread == 0)
        {
            throw new Exception($"Failed to create thread for '{entryPoint}'/{entryAddr:X}");
        }

        try
        {
            int entryWaitResult = NativeMethods.WaitForSingleObject(entryThread);

            if (entryWaitResult != 0)
            {
                throw new Exception($"Entry thread failed: {entryWaitResult:X}");
            }
        }
        finally
        {
            NativeMethods.CloseHandle(entryThread);
        }
    }

    private static bool ProcessContainsModule(Process process, string modulePath)
    {
        for (int i = 0; i < process.Modules.Count; i++)
        {
            if (process.Modules[i].FileName == modulePath)
            {
                return true;
            }
        }

        return false;
    }
}
