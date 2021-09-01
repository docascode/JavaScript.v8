using System;
using System.Runtime.InteropServices;

namespace Microsoft.Docs.Build
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct JsIsolate
    {
        private readonly IntPtr Pointer;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct JsonWriteStringState
    {
        private readonly IntPtr Pointer;
    };

    internal delegate void JsonWriteInt(long value);
    internal delegate void JsonWriteNumber(double value);
    internal unsafe delegate void JsonWriteStringDelegate(nuint length, JsonWriteStringCopyDelegate copy, JsonWriteStringState state);
    internal unsafe delegate void JsonWriteStringCopyDelegate(char* buffer, nuint length, JsonWriteStringState state);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct JsonWriter
    {
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_undefined;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_null;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_true;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_false;
        [MarshalAs(UnmanagedType.FunctionPtr)] public JsonWriteInt write_int;
        [MarshalAs(UnmanagedType.FunctionPtr)] public JsonWriteNumber write_number;
        [MarshalAs(UnmanagedType.FunctionPtr)] public JsonWriteStringDelegate write_string;
        [MarshalAs(UnmanagedType.FunctionPtr)] public JsonWriteInt write_start_array;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_end_array;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_start_object;
        [MarshalAs(UnmanagedType.FunctionPtr)] public JsonWriteStringDelegate write_property_name;
        [MarshalAs(UnmanagedType.FunctionPtr)] public Action write_end_object;
    };

    internal static unsafe class NativeMethods
    {
        [DllImport("docfxjsv8")]
        public static extern JsIsolate js_new_isolate();

        [DllImport("docfxjsv8")]
        public static extern int js_run(JsIsolate isolate, char* code, nuint length, ref JsonWriter writer);
    }
}
