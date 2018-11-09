using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Frida.NetStandard
{
    static class Runtime
    {
        public const string LibName = "Frida.Exports.dll";
        static volatile int refs = 0;

        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_init();
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_deinit();
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_g_free(IntPtr ptr);

        public static void Ref()
        {
            if (Interlocked.Increment(ref refs) == 1)
            {
                Console.WriteLine("Initializing frida");
                _frida_init();
            }
        }

        public static void Unref()
        {

            if (Interlocked.Decrement(ref refs) <= 0)
            {
                Console.WriteLine("Deinitializing frida");
                _frida_deinit();
            }
        }
        public static string ReadStringAndFree(this IntPtr ptr)
        {
            var ret = Marshal.PtrToStringAuto(ptr);
            _frida_g_free(ptr);
            return ret;
        }
    }
}
