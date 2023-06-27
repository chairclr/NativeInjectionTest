using System.Diagnostics;

namespace NativeInjector;

internal class Program
{
    static void Main(string[] args)
    {
        string processNameOrId;
        string path;
        string entryPoint = "NativeEntry";

        if (args.Length == 2)
        {
            processNameOrId = args[0];
            path = args[1];
        }
        else if (args.Length == 3)
        {
            processNameOrId = args[0];
            path = args[1];
            entryPoint = args[2];
        }
        else
        {
            Console.WriteLine("Invalid Arguments");
            Console.WriteLine("Usage:");
            Console.WriteLine("./NativeInjector targetProcessNameOrId /path/to/dll/file ?optionalEntryPointName");

            return;
        }

        Process? process;

        if (int.TryParse(processNameOrId, out int processId))
        {
            process = Process.GetProcessById(processId);
        }
        else
        {
            Process[] processes = Process.GetProcessesByName(processNameOrId);

            if (processes.Length < 1)
            {
                Console.WriteLine($"No process named '{processNameOrId}'");
                return;
            }
            else if (processes.Length > 1)
            {
                Console.WriteLine($"Mulitple processes named named '{processNameOrId}'");
                return;
            }
            else
            {
                process = processes[0];
            }
        }

        Injector.InjectIntoProcess(process, path, entryPoint);
    }
}
