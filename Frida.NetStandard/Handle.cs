using System;
using System.Runtime.InteropServices;

namespace Frida.NetStandard
{
    class Handle
    {

        public IntPtr Pointer { get; private set; }

        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_g_object_unref(IntPtr ptr);

        public Handle(Func<IntPtr> handle)
        {
            try
            {
                Runtime.Ref();
                Pointer = handle();
                if (Pointer == IntPtr.Zero)
                    Runtime.Unref();
            }
            catch
            {
                Runtime.Unref();
                throw;
            }
        }

        ~Handle()
        {
            if (Pointer == IntPtr.Zero)
                return;
            _frida_g_object_unref(Pointer);
            Runtime.Unref();
        }
    }
}
