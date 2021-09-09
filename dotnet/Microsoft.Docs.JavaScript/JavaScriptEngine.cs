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

        public void Run(JavaScriptValueAction action)
        {
            js_run_in_context(_isolate, action);
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
