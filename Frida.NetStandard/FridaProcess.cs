using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Frida.NetStandard
{
    public class FridaProcess
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_get_name(IntPtr device);

        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern uint _frida_process_get_pid(IntPtr device);

        Handle handle;
        public FridaProcess(Func<IntPtr> builder)
        {
            handle = new Handle(builder);
        }

        public string Name => _frida_device_get_name(handle.Pointer).ReadStringAndFree();
        public uint Pid => _frida_process_get_pid(handle.Pointer);

        public byte[] SmallIcon => throw new NotImplementedException();
        public byte[] LargeIcon => throw new NotImplementedException();

        public override string ToString()
            => $"{Name} ({Pid})";
    }

}
