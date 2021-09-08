using System;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public delegate JavaScriptValue JavaScriptFunction(JavaScriptScope scope, JavaScriptValue self, ReadOnlySpan<JavaScriptValue> args);
    public delegate void JavaScriptScopeAction(JavaScriptScope scope);
    public delegate void JavaScriptScopeAction<T>(JavaScriptScope scope, T arg0);

    public unsafe readonly ref struct JavaScriptScope
    {
        private const int MaxStackAllocArgs = 10;

        private readonly IntPtr _scope;

        internal JavaScriptScope(IntPtr scope) => _scope = scope;

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

        public JavaScriptValue CreateFunction(JavaScriptFunction function)
        {
            return new(_scope, js_function_new(_scope, Callback));

            IntPtr Callback(IntPtr scope, IntPtr self, IntPtr* argv, nint argc)
            {
                Span<JavaScriptValue> args = argc <= MaxStackAllocArgs
                    ? stackalloc JavaScriptValue[MaxStackAllocArgs].Slice(0, (int)argc)
                    : new JavaScriptValue[argc];

                for (var i = 0; i < argc; i++)
                {
                    args[i] = new(scope, argv[i]);
                }

                return function(new(scope), new(scope, self), args)._value;
            }
        }

        public void RunScript(string code, string filename, JavaScriptScopeAction<JavaScriptValue> error, JavaScriptScopeAction<JavaScriptValue> result)
        {
            js_run_script(
                _scope,
                ToJsString(_scope, code),
                ToJsString(_scope, filename),
                (scope, value) => error(new(scope), new(scope, value)),
                (scope, value) => result(new(scope), new(scope, value)));
        }
    }
}
