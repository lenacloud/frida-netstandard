using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Frida.NetStandard
{
    public enum FridaDeviceType
    {
        Local = 0,
        Remote = 1,
        USB = 2,
    }
    public class Device
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_get_name(IntPtr device);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_get_id(IntPtr device);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern bool _frida_device_is_lost(IntPtr device);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern FridaDeviceType _frida_device_get_dtype(IntPtr device);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_enumerate_processes_sync(IntPtr self, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern int _frida_process_list_size(IntPtr list);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_process_list_get(IntPtr list, Int32 index);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_device_resume_sync(IntPtr self, UInt32 pid, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_attach_sync(IntPtr self, UInt32 pid, out IntPtr error);

        Handle handle;

        public Device(Func<IntPtr> builder)
        {
            handle = new Handle(builder);
        }

        public string Name => _frida_device_get_name(handle.Pointer).ReadStringAndFree();
        public string Id => _frida_device_get_id(handle.Pointer).ReadStringAndFree();
        public bool IsLost => _frida_device_is_lost(handle.Pointer);
        public FridaDeviceType Type => _frida_device_get_dtype(handle.Pointer);
        public byte[] Icon => throw new NotImplementedException();
        public List<FridaProcess> EnumerateProcesses()
        {
            IntPtr error;
            using (var devices = new Ref(_frida_device_enumerate_processes_sync(handle.Pointer, out error)))
            {
                GError.Throw(error);
                var len = _frida_process_list_size(devices.Pointer);
                var ret = new List<FridaProcess>();
                for (int i = 0; i < len; i++)
                {
                    ret.Add(new FridaProcess(() => _frida_process_list_get(devices.Pointer, i)));
                }
                return ret;
            }
        }

        public uint Spawn()
            => throw new NotImplementedException();


        public void Resume(uint pid)
        {
            IntPtr error;
            _frida_device_resume_sync(handle.Pointer, pid, out error);
            GError.Throw(error);
        }

        public FridaSession Attach(uint pid)
        {
            return new FridaSession(() =>
            {
                IntPtr error;
                var session = _frida_device_attach_sync(handle.Pointer, pid, out error);
                GError.Throw(error);
                return session;
            });
        }

        public override string ToString()
        {
            return "Device " + handle.Pointer;
        }
    }
}
