using System;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public unsafe readonly struct JavaScriptContext
    {
        private readonly IntPtr _scope;

        internal JavaScriptContext(IntPtr scope) => _scope = scope;

        public JavaScriptValue CreateUndefined() => new(_scope, js_undefined(_scope));

        public JavaScriptValue CreateNull() => new(_scope, js_null(_scope));

        public JavaScriptValue CreateTrue() => new(_scope, js_true(_scope));

        public JavaScriptValue CreateFalse() => new(_scope, js_false(_scope));

        public JavaScriptValue CreateInteger(int value) => new(_scope, js_integer_new(_scope, value));

        public JavaScriptValue CreateNumber(double value) => new(_scope, js_number_new(_scope, value));

        public JavaScriptValue CreateArray(int length) => new(_scope, js_array_new(_scope, length));
        
        public JavaScriptValue CreateObject() => new(_scope, js_object_new(_scope));

        public JavaScriptValue CreateString(string value)
        {
            fixed (char* chars = value)
            {
                return new(_scope, js_string_new(_scope, chars, value.Length));
            }
        }

        public void RunScript(string code, string filename, Action<JavaScriptValue> error, Action<JavaScriptValue> result)
        {
            js_run_script(
                _scope,
                ToJsString(_scope, code),
                ToJsString(_scope, filename),
                (scope, value) => error(new(scope, value)),
                (scope, value) => result(new(scope, value)));
        }
    }
}
