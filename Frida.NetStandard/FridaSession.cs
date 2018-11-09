using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Frida.NetStandard
{
    public class FridaSession
    {
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern Int32 _frida_session_get_pid(IntPtr self);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern void _frida_session_detach_sync(IntPtr self);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_session_create_script_sync(IntPtr self, string name, string source, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_session_enable_debugger_sync(IntPtr self, UInt16 port, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_session_disable_debugger_sync(IntPtr self, out IntPtr error);
        [DllImport(Runtime.LibName, CharSet = CharSet.Unicode)]
        static extern IntPtr _frida_session_enable_jit_sync(IntPtr self, out IntPtr error);

        Handle handle;
        public FridaSession(Func<IntPtr> builder)
        {
            handle = new Handle(builder);
        }

        public int Pid => _frida_session_get_pid(handle.Pointer);
        public void Dettach()
            => _frida_session_detach_sync(handle.Pointer);

        public ScriptWithRpc<TRpc> CreateScriptWithRpc<TRpc>(string source)
            => CreateScriptWithRpc<TRpc>(null, source);

        public ScriptWithRpc<TRpc> CreateScriptWithRpc<TRpc>(string name, string source)
        {
            return new ScriptWithRpc<TRpc>(() =>
            {
                IntPtr error;
                var script = _frida_session_create_script_sync(handle.Pointer, name, source, out error);
                GError.Throw(error);
                return script;
            });
        }


        public Script CreateScript(string source)
            => CreateScript(null, source);
        public Script CreateScript(string name, string source)
        {
            return new Script(() =>
            {
                IntPtr error;
                var script = _frida_session_create_script_sync(handle.Pointer, name, source, out error);
                GError.Throw(error);
                return script;
            });
        }

        public void EnableDebugger(UInt16 port = 0)
        {
            IntPtr error;
            _frida_session_enable_debugger_sync(handle.Pointer, port, out error);
            GError.Throw(error);
        }

        public void DisableDebugger()
        {
            IntPtr error;
            _frida_session_disable_debugger_sync(handle.Pointer, out error);
            GError.Throw(error);
        }

        public void EnableJit()
        {
            IntPtr error;
            _frida_session_enable_jit_sync(handle.Pointer, out error);
            GError.Throw(error);
        }
    }

}
