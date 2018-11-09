using System;
using System.Runtime.InteropServices;

namespace Frida.NetStandard
{
    [StructLayout(LayoutKind.Sequential)]
    struct GError
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_g_clear_error(ref IntPtr ptr);

        public Int32 GQuark;
        public Int32 Code;
        public string Message;
        public static void Throw(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return;
            var err = (GError)Marshal.PtrToStructure(ptr, typeof(GError));
            _frida_g_clear_error(ref ptr);
            throw new InvalidOperationException(err.Message ?? "Unknown frida error");
        }
    }
}
