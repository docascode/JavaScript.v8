using System.Runtime.InteropServices;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build;

public delegate JavaScriptValue JavaScriptFunction(JavaScriptScope scope, JavaScriptValue self, ReadOnlySpan<JavaScriptValue> args);
public delegate void JavaScriptScopeAction(JavaScriptScope scope, JavaScriptValue value);
public delegate T JavaScriptScopeAction<T>(JavaScriptScope scope, JavaScriptValue value);

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly ref struct JavaScriptScope
{
    internal readonly IntPtr _ptr;

    public JavaScriptValue CreateUndefined() => js_undefined(this);

    public JavaScriptValue CreateNull() => js_null(this);

    public JavaScriptValue CreateTrue() => js_true(this);

    public JavaScriptValue CreateFalse() => js_false(this);

    public JavaScriptValue CreateInteger(int value) => js_integer_new(this, value);

    public JavaScriptValue CreateNumber(double value) => js_number_new(this, value);

    public JavaScriptValue CreateArray(int length) => js_array_new(this, length);

    public JavaScriptValue CreateObject() => js_object_new(this);

    public JavaScriptValue CreateString(string value)
    {
        fixed (char* chars = value)
        {
            return js_string_new(this, chars, value.Length);
        }
    }

    public JavaScriptValue CreateFunction(JavaScriptFunction function)
    {
        return js_function_new(this, Callback);

        int Callback(JavaScriptScope scope, JavaScriptValue self, JavaScriptValue* argv, nint argc, out JavaScriptValue result)
        {
            try
            {
                var args = new Span<JavaScriptValue>(argv, (int)argc);
                result = function(scope, self, args);
                return 0;
            }
            catch (Exception ex)
            {
                result = scope.CreateString(ex.ToString());
                return -1;
            }
        }
    }

    public JavaScriptValue RunScript(string code, string filename)
    {
        return js_run_script(this, ToJsString(this, code), ToJsString(this, filename), out var result) == 0
            ? result
            : throw new JavaScriptException(result.AsString(this));
    }
}
