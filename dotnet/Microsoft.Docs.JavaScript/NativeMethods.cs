using System;
using System.Runtime.InteropServices;

namespace Microsoft.Docs.Build
{
    internal static class NativeMethods
    {
        [DllImport("docfxjsv8")]
        public static extern IntPtr js_new_isolate();
    }
}
