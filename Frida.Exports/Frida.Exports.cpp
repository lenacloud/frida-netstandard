// Frida.NetStandard.cpp : Defines the exported functions for the DLL application.
//
#include "frida-core.h"

#define LIBEXPORT extern "C" __declspec(dllexport)

wchar_t * UTF8CStringToClrString(const char * str)
{
	wchar_t * strUtf16 = reinterpret_cast<wchar_t *> (g_utf8_to_utf16(str, -1, NULL, NULL, NULL));
	return strUtf16;
}
gchar * ClrStringToUTF8CString(const wchar_t * strUtf16)
{
	gchar * strUtf8 = g_utf16_to_utf8(reinterpret_cast<const gunichar2 *> (strUtf16), -1, NULL, NULL, NULL);
	return strUtf8;
}

LIBEXPORT void _frida_g_free(gpointer ptr) { g_free(ptr); }
LIBEXPORT void _frida_g_clear_error(GError** error) { g_clear_error(error); }
LIBEXPORT void _frida_g_object_unref(gpointer ptr) { g_object_unref(ptr); }

LIBEXPORT void _frida_init() { frida_init(); }
LIBEXPORT void _frida_deinit() { frida_deinit(); }

// === device manager
LIBEXPORT FridaDeviceManager* _frida_device_manager_new() { return frida_device_manager_new(); }
LIBEXPORT void _frida_device_manager_close_sync(FridaDeviceManager * self) { frida_device_manager_close_sync(self); }
LIBEXPORT FridaDeviceList* _frida_device_manager_enumerate_devices_sync(FridaDeviceManager * self, GError** errorMessage) { return frida_device_manager_enumerate_devices_sync(self, errorMessage); }
LIBEXPORT int _frida_device_list_size(FridaDeviceList* list) { return frida_device_list_size(list); }
LIBEXPORT FridaDevice* _frida_device_list_get(FridaDeviceList* list, gint index) { return frida_device_list_get(list, index); }

// === device
LIBEXPORT gboolean _frida_device_is_lost(FridaDevice* device) { return frida_device_is_lost(device); }
LIBEXPORT wchar_t *  _frida_device_get_name(FridaDevice* device) { return UTF8CStringToClrString(frida_device_get_name(device)); }
LIBEXPORT wchar_t *  _frida_device_get_id(FridaDevice* device) { return UTF8CStringToClrString(frida_device_get_id(device)); }
LIBEXPORT FridaDeviceType _frida_device_get_dtype(FridaDevice* device) { return frida_device_get_dtype(device); }
LIBEXPORT FridaProcessList* _frida_device_enumerate_processes_sync(FridaDevice * self, GError** errorMessage) { return frida_device_enumerate_processes_sync(self, errorMessage); }
LIBEXPORT int _frida_process_list_size(FridaProcessList* list) { return frida_process_list_size(list); }
LIBEXPORT FridaProcess* _frida_process_list_get(FridaProcessList* list, gint index) { return frida_process_list_get(list, index); }
LIBEXPORT void _frida_device_resume_sync(FridaDevice* self, guint pid, GError** error) { frida_device_resume_sync(self, pid, error); }
LIBEXPORT FridaSession* _frida_device_attach_sync(FridaDevice* self, guint pid, GError** error) { return frida_device_attach_sync(self, pid, error); }


// === process
LIBEXPORT wchar_t *  _frida_process_get_name(FridaProcess* self) { return UTF8CStringToClrString(frida_process_get_name(self)); }
LIBEXPORT guint _frida_process_get_pid(FridaProcess* self) { return frida_process_get_pid(self); }

// === session
LIBEXPORT guint _frida_session_get_pid(FridaSession * self) { return frida_session_get_pid(self); }
LIBEXPORT void _frida_session_detach_sync(FridaSession * self) { frida_session_detach_sync(self); }
LIBEXPORT FridaScript* _frida_session_create_script_sync(FridaSession* self, wchar_t * name, wchar_t * source, GError** error) {

	gchar* nameConv = (name != nullptr) ? ClrStringToUTF8CString(name) : NULL;
	gchar* sourceConv = ClrStringToUTF8CString(source);
	FridaScript* script = frida_session_create_script_sync(self, nameConv, sourceConv, error);
	g_free(nameConv);
	g_free(sourceConv);
	return script;
}
LIBEXPORT void _frida_session_enable_debugger_sync(FridaSession* self, guint16 port, GError** error) { frida_session_enable_debugger_sync(self, port, error); }
LIBEXPORT void _frida_session_disable_debugger_sync(FridaSession* self, GError** error) { frida_session_disable_debugger_sync(self, error); }
LIBEXPORT void _frida_session_enable_jit_sync(FridaSession* self, GError** error) { frida_session_enable_jit_sync(self, error); }


// === script
LIBEXPORT void _frida_script_load_sync(FridaScript* self, GError** error) { frida_script_load_sync(self, error); }
LIBEXPORT void _frida_script_unload_sync(FridaScript* self, GError** error) { frida_script_unload_sync(self, error); }
LIBEXPORT void _frida_script_eternalize_sync(FridaScript* self, GError** error) { frida_script_eternalize_sync(self, error); }
LIBEXPORT void _frida_script_post_sync(FridaScript* self, const wchar_t* message, gconstpointer data, gsize dataLength, GError** error) {

	gchar * messageUtf8 = ClrStringToUTF8CString(message);
	GBytes * dataBytes = g_bytes_new(data, dataLength);
	frida_script_post_sync(self, messageUtf8, dataBytes, error);
	g_bytes_unref(dataBytes);
	g_free(messageUtf8);
}

typedef void(__stdcall *OnScriptMessageDelegate)(wchar_t* message, gconstpointer data, gsize size);
static void OnScriptMessage(FridaScript * script, const gchar * messageUtf8, GBytes * bytes, gpointer user_data)
{
	//msclr::gcroot<Script ^> * wrapper = static_cast<msclr::gcroot<Script ^> *> (user_data);
	wchar_t* message = UTF8CStringToClrString(messageUtf8);
	gsize size = 0;
	gconstpointer data = nullptr;
	if (bytes != NULL)
		data = g_bytes_get_data(bytes, &size);
	OnScriptMessageDelegate fn = static_cast<OnScriptMessageDelegate>(user_data);
	fn(message, data, size);
}
LIBEXPORT void _frida_script_connect_message_handler(gpointer handle, OnScriptMessageDelegate handler) {
	g_signal_connect(handle, "message", G_CALLBACK(OnScriptMessage), handler);
}
LIBEXPORT void _frida_script_disconnect_message_handler(gpointer handle, OnScriptMessageDelegate handler) {
	g_signal_handlers_disconnect_by_func(handle, OnScriptMessage, handler);
}