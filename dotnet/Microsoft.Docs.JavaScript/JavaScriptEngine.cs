using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public unsafe class JavaScriptEngine
    {
        private readonly JsIsolate _isolate;

        public JavaScriptEngine()
        {
            _isolate = js_new_isolate();
        }

        public void Run(string code, string methodName, IJsonReader reader, IJsonWriter writer)
        {
            var jsonWriter = CreateWriter(writer);

            fixed (char* pCode = code)
            {
                js_run(_isolate, pCode, (nuint)code.Length, ref jsonWriter);
            }
        }

        private JsonWriter CreateWriter(IJsonWriter writer)
        {
            return new()
            {
                write_undefined = writer.WriteUndefined,
                write_null = writer.WriteNull,
                write_true = writer.WriteTrue,
                write_false = writer.WriteFalse,
                write_int = writer.WriteInt,
                write_number = writer.WriteNumber,
                write_string = WriteString,
                write_start_array = writer.WriteStartArray,
                write_end_array = writer.WriteEndArray,
                write_start_object = writer.WriteStartObject,
                write_property_name = WritePropertyName,
                write_end_object = writer.WriteEndObject,
            };

            void WriteString(nuint length, JsonWriteStringCopyDelegate copy, JsonWriteStringState state)
            {
                writer.WriteString(string.Create((int)length, (copy, length, state), static (buffer, state) =>
                {
                    fixed (char* pBuffer = buffer)
                    {
                        state.copy(pBuffer, state.length, state.state);
                    }
                }));
            }

            void WritePropertyName(nuint length, JsonWriteStringCopyDelegate copy, JsonWriteStringState state)
            {
                writer.WritePropertyName(string.Create((int)length, (copy, length, state), static (buffer, state) =>
                {
                    fixed (char* pBuffer = buffer)
                    {
                        state.copy(pBuffer, state.length, state.state);
                    }
                }));
            }
        }
    }
}
