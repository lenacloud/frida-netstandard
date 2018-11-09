using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Frida.NetStandard
{
    public class DeviceManager
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_manager_new();
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_device_manager_close_sync(IntPtr deviceManager);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_manager_enumerate_devices_sync(IntPtr deviceManager, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern int _frida_device_list_size(IntPtr list);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_device_list_get(IntPtr list, Int32 index);

        Handle handle;

        public DeviceManager()
        {
            handle = new Handle(_frida_device_manager_new);
        }

        public List<Device> EnumerateDevices()
        {

            IntPtr error;
            using (var devices = new Ref(_frida_device_manager_enumerate_devices_sync(handle.Pointer, out error)))
            {
                GError.Throw(error);
                var len = _frida_device_list_size(devices.Pointer);
                var ret = new List<Device>();
                for (int i = 0; i < len; i++)
                {
                    ret.Add(new Device(() => _frida_device_list_get(devices.Pointer, i)));
                }
                return ret;
            }
        }
    }

}
