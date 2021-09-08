using System;
using System.Threading;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public unsafe sealed class JavaScriptEngine : IDisposable
    {
        private IntPtr _isolate;

        public JavaScriptEngine()
        {
            _isolate = js_isolate_new();
        }

        public void Run(Action<JavaScriptContext> action)
        {
            js_run_in_context(_isolate, scope => action(new(scope)));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (Interlocked.CompareExchange(ref _isolate, default, default) != default)
            {
                js_isolate_delete(_isolate);
            }
        }
    }
}
