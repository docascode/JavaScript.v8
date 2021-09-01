using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Docs.Build;

var writer = new MyJsonWriter();
new JavaScriptEngine().Run("1+2", "", null, writer);

Console.WriteLine(writer.ToString());

class MyJsonWriter : IJsonWriter
{
    private readonly ArrayBufferWriter<byte> _buffer = new ArrayBufferWriter<byte>();
    private readonly Utf8JsonWriter _writer;

    public MyJsonWriter() => _writer = new Utf8JsonWriter(_buffer);

    public override string ToString() => Encoding.UTF8.GetString(_buffer.WrittenSpan);

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