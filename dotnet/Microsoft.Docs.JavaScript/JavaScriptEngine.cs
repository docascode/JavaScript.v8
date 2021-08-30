using System;
using System.Runtime.InteropServices;
using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public class JavaScriptEngine
    {
        private readonly IntPtr _isolate;

        public JavaScriptEngine()
        {
            _isolate = js_new_isolate();
        }
    }
}
