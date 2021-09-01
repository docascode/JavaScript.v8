using System.Buffers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JavaScriptEngineTest : IClassFixture<JavaScriptEngine>
    {
        private readonly JavaScriptEngine _js;

        public JavaScriptEngineTest(JavaScriptEngine js) => _js = js;

        [Theory]
        [InlineData("1 + 2", "3")]
        [InlineData("1 + 'a'", "'1a'")]
        [InlineData("null", "null")]
        [InlineData("undefined", "null")]
        [InlineData("'hello' + ' world'", "'hello world'")]
        [InlineData("[1,3.14,21474836470,-2147483647]", "[1,3.14,21474836470,-2147483647]")]
        [InlineData("{a:0,b:{c:[]}}", "{'a':0,'b':{'c':[]}}")]
        public void RunJavaScript(string code, string output)
        {
            var writer = new JsonWriter();
            _js.Run(code, "", null, writer);
            Assert.Equal(output, writer.GetString().Replace('\"', '\''));
        }

        private class JsonWriter : IJsonWriter
        {
            private readonly ArrayBufferWriter<byte> _buffer = new ArrayBufferWriter<byte>();
            private readonly Utf8JsonWriter _writer;

            public JsonWriter() => _writer = new Utf8JsonWriter(_buffer);

            public string GetString()
            {
                _writer.Flush();
                return Encoding.UTF8.GetString(_buffer.WrittenSpan);
            }

            public void Flush() => _writer.Flush();

            public void WriteEndArray() => _writer.WriteEndArray();

            public void WriteEndObject() => _writer.WriteEndObject();

            public void WriteFalse() => _writer.WriteBooleanValue(false);

            public void WriteInt(long value) => _writer.WriteNumberValue(value);

            public void WriteNull() => _writer.WriteNullValue();

            public void WriteNumber(double value) => _writer.WriteNumberValue(value);

            public void WritePropertyName(string name) => _writer.WritePropertyName(name);

            public void WriteStartArray(long length) => _writer.WriteStartArray();

            public void WriteStartObject() => _writer.WriteStartObject();

            public void WriteString(string value) => _writer.WriteStringValue(value);

            public void WriteTrue() => _writer.WriteBooleanValue(true);

            public void WriteUndefined() => _writer.WriteNullValue();
        }
    }
}
