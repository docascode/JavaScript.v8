using System;
using System.Runtime.ExceptionServices;
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

        public void Run(JavaScriptScopeAction action)
        {
            ExceptionDispatchInfo exception = null;

            js_run_in_context(_isolate, (scope, global) =>
            {
                try
                {
                    action(scope, global);
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                }
            });

            exception?.Throw();
        }

        public T Run<T>(JavaScriptScopeAction<T> action)
        {
            T result = default;
            ExceptionDispatchInfo exception = null;

            js_run_in_context(_isolate, (scope, global) =>
            {
                try
                {
                    result = action(scope, global);
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                }
            });

            exception?.Throw();
            return result;
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
