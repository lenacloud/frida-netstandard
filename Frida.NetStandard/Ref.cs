using System;
using System.Runtime.InteropServices;

namespace Frida.NetStandard
{
    class Ref : IDisposable
    {
        public IntPtr Pointer { get; private set; }
        bool disposed = false;

        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_g_object_unref(IntPtr ptr);

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (Pointer != IntPtr.Zero)
                _frida_g_object_unref(Pointer);
            Runtime.Unref();
        }

        public Ref(IntPtr handle)
        {
            Pointer = handle;
        }
    }

}
