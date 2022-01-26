using System.Runtime.InteropServices;

namespace JavaScript.v8;

internal unsafe delegate int JsFunction(JavaScriptScope scope, JavaScriptValue self, JavaScriptValue* argv, nint argc, out JavaScriptValue result);

internal static unsafe class NativeMethods
{
    private const string LibName = "jsv8";

    [DllImport(LibName)]
    public static extern JavaScriptValueType js_value_type(JavaScriptValue value);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_undefined(JavaScriptScope scope);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_null(JavaScriptScope scope);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_true(JavaScriptScope scope);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_false(JavaScriptScope scope);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_integer_new(JavaScriptScope scope, int value);

    [DllImport(LibName)]
    public static extern long js_integer_value(JavaScriptScope scope, JavaScriptValue value);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_number_new(JavaScriptScope scope, double value);

    [DllImport(LibName)]
    public static extern double js_number_value(JavaScriptScope scope, JavaScriptValue value);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_string_new(JavaScriptScope scope, char* chars, nint length);

    [DllImport(LibName)]
    public static extern nint js_string_length(JavaScriptValue value);

    [DllImport(LibName)]
    public static extern nint js_string_copy(IntPtr scope, JavaScriptValue value, char* buffer, nint length);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_array_new(JavaScriptScope scope, int length);

    [DllImport(LibName)]
    public static extern uint js_array_length(JavaScriptValue array);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_array_get_index(JavaScriptScope scope, JavaScriptValue array, uint index);

    [DllImport(LibName)]
    public static extern void js_array_set_index(JavaScriptScope scope, JavaScriptValue array, uint index, JavaScriptValue value);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_object_new(JavaScriptScope scope);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_object_get_own_property_names(JavaScriptScope scope, JavaScriptValue obj);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_object_get_property(JavaScriptScope scope, JavaScriptValue obj, JavaScriptValue key);

    [DllImport(LibName)]
    public static extern void js_object_set_property(JavaScriptScope scope, JavaScriptValue obj, JavaScriptValue key, JavaScriptValue value);

    [DllImport(LibName)]
    public static extern JavaScriptValue js_function_new(JavaScriptScope scope, JsFunction callback);

    [DllImport(LibName)]
    public static extern int js_function_call(JavaScriptScope scope, JavaScriptValue value, JavaScriptValue recv, JavaScriptValue* argv, nint argc, out JavaScriptValue result);

    [DllImport(LibName)]
    public static extern IntPtr js_isolate_new();

    [DllImport(LibName)]
    public static extern void js_isolate_delete(IntPtr isolate);

    [DllImport(LibName)]
    public static extern void js_run_in_context(IntPtr isolate, JavaScriptScopeAction callback);

    [DllImport(LibName)]
    public static extern int js_run_script(JavaScriptScope scope, JavaScriptValue code, JavaScriptValue filename, out JavaScriptValue result);

    public static JavaScriptValue ToJsString(JavaScriptScope scope, string value)
    {
        fixed (char* chars = value)
        {
            return js_string_new(scope, chars, value.Length);
        }
    }

    public static string FromJsString(JavaScriptScope scope, JavaScriptValue str)
    {
        var length = js_string_length(str);

        return string.Create((int)length, (scope._ptr, str, length), static (buffer, state) =>
        {
            fixed (char* buf = buffer)
            {
                js_string_copy(state._ptr, state.str, buf, state.length);
            }
        });
    }
}
