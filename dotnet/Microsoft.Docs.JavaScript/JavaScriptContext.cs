using System;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public delegate void JavaScriptContextAction(JavaScriptContext context);
    public delegate void JavaScriptValueAction(JavaScriptValue value);

    public unsafe ref struct JavaScriptContext
    {
        private readonly IntPtr _scope;

        internal JavaScriptContext(IntPtr scope) => _scope = scope;

        public void RunScript(string code, string filename, JavaScriptValueAction error, JavaScriptValueAction result)
        {
            js_run_script(
                _scope,
                ToJsString(_scope, code),
                ToJsString(_scope, filename),
                (scope, value) => error(new JavaScriptValue(scope, value)),
                (scope, value) => result(new JavaScriptValue(scope, value)));
        }
    }
}
