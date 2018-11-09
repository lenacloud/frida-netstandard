using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Frida.NetStandard
{
    public class Script
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_load_sync(IntPtr device, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_unload_sync(IntPtr device, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_eternalize_sync(IntPtr device, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_post_sync(IntPtr device, string message, IntPtr data, UInt32 dataLength, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_connect_message_handler(IntPtr handle, IntPtr fnPointer);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_script_disconnect_message_handler(IntPtr handle, IntPtr fnPointer);

        delegate void OnScriptMessageDelegate(IntPtr message, IntPtr data, UInt32 dataSize);
        Handle handle;
        IntPtr onMessageDelegate;
        GCHandle onMessageDelegateHandle;

        public Script(Func<IntPtr> builder)
        {
            handle = new Handle(builder);
            OnScriptMessageDelegate del = OnScriptMessage;
            onMessageDelegateHandle = GCHandle.Alloc(del);
            onMessageDelegate = Marshal.GetFunctionPointerForDelegate(del);
            _frida_script_connect_message_handler(handle.Pointer, onMessageDelegate);
        }

        ~Script()
        {
            if (handle != null && onMessageDelegate != IntPtr.Zero)
            {
                _frida_script_disconnect_message_handler(handle.Pointer, onMessageDelegate);
                onMessageDelegateHandle.Free();
            }
        }

        //  {"type":"log","level":"info","payload":"Script initialized"}
        public delegate void MessageDelegate(string type, JToken payload, byte[] data);
        public event MessageDelegate OnMessage;
        public delegate void ConsoleDelegate(string level, string data);
        public event ConsoleDelegate OnConsole;



        void OnScriptMessage(IntPtr messagePtr, IntPtr data, UInt32 dataSize)
        {
            try
            {
                var message = messagePtr.ReadStringAndFree();
                byte[] dataBytes = null;
                if (data != IntPtr.Zero)
                {
                    dataBytes = new byte[dataSize];
                    Marshal.Copy(data, dataBytes, 0, (int)dataSize);
                }

                var payload = JsonConvert.DeserializeObject<JObject>(message);
                JToken type;
                if (!payload.TryGetValue("type", out type) || type.Type != JTokenType.String)
                    return;
                ProcessMessage(type.Value<string>(), payload, dataBytes);

            } catch (Exception ex)
            {
                OnConsole?.Invoke("error", "ERROR PROCESSING MESSAGE: " + ex.Message);
            }
        }

        class FridaLog
        {
            public string level { get; set; }
            public string payload { get; set; }
        }
        protected virtual void ProcessMessage(string type, JObject data, byte[] dataBytes)
        {
            switch (type)
            {
                case "log":
                    var msg = data.ToObject<FridaLog>();
                    OnConsole?.Invoke(msg.level, msg.payload);
                    break;
                default:
                    JToken payload;
                    data.TryGetValue("payload", out payload);
                    OnMessage?.Invoke(type, payload, dataBytes);
                    break;
            }
        }

        public void Load()
        {
            IntPtr error;
            _frida_script_load_sync(handle.Pointer, out error);
            GError.Throw(error);
        }

        public void Unload()
        {
            IntPtr error;
            _frida_script_unload_sync(handle.Pointer, out error);
            GError.Throw(error);
        }

        public void Eternalize()
        {
            IntPtr error;
            _frida_script_eternalize_sync(handle.Pointer, out error);
            GError.Throw(error);
        }

        public void Post(object payload, byte[] data = null)
        {
            IntPtr dataPtr = IntPtr.Zero;
            try
            {
                var message = JsonConvert.SerializeObject(payload);
                if (data != null)
                {
                    dataPtr = Marshal.AllocHGlobal(data.Length);
                    Marshal.Copy(data, 0, dataPtr, data.Length);
                }
                IntPtr error;
                _frida_script_post_sync(handle.Pointer, message, dataPtr, (uint)(data?.Length ?? 0), out error);
                GError.Throw(error);
            }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(dataPtr);
            }
        }
    }

}
