using Frida.NetStandard;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TestProgram
{


    public class Msg
    {
        public string message { get; set; }
        public int number { get; set; }
    }
    public class Input
    {
        public string author { get; set; }
    }

    public interface IRpc
    {
        Msg pingScript(Input input);

        // supports Task results for long operations
        Task longOperation();
    }

    class Program
    {

        // for windows
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        static void Main(string[] args)
        {
            // register dll directory (so the frida DLL can be found)
            var dir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "..", "..", "..", "..", "x86", "Debug"));
            if (!dir.Exists)
                throw new InvalidOperationException("Build Frida.Exports first");
            SetDllDirectory(dir.FullName);

            var man = new DeviceManager();
            var notepad = Process.Start("notepad");
            try
            {
                // attach to local 'notepad'
                var device = man.EnumerateDevices().First(x => x.Type == FridaDeviceType.Local);
                var session = device.Attach((uint)notepad.Id);
                Console.WriteLine("Attached to " + notepad.Id);

                // inject script
                var script = session.CreateScriptWithRpc<IRpc>(File.ReadAllText("script.js"));
                script.OnMessage += (type, msg, data) => Console.WriteLine("Received message: " + msg);
                script.OnConsole += (level, msg) => Console.WriteLine($"[frida {level}] {msg}");

                script.Load();

                // perform a synchronous RPC call
                var pong = script.Rpc?.pingScript(new Input { author = "c#" });
                Console.WriteLine("RPC answer: " + pong?.message);

                // perform an async RPC call, non blocking.
                script.Rpc.longOperation()
                    .ContinueWith(t => Console.WriteLine("End of long operation"));

                Console.WriteLine("===== Press ESC to quit ======");
                while (Console.ReadKey().Key != ConsoleKey.Escape);

            }
            finally
            {
                notepad.Kill();
            }
        }
    }
}
