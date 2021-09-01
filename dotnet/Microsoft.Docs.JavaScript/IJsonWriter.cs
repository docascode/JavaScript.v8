namespace Microsoft.Docs.Build
{
    public interface IJsonWriter
    {
        void WriteUndefined();
        void WriteNull();
        void WriteTrue();
        void WriteFalse();
        void WriteInt(long value);
        void WriteNumber(double value);
        void WriteString(string value);
        void WriteStartArray(long length);
        void WriteEndArray();
        void WriteStartObject();
        void WritePropertyName(string name);
        void WriteEndObject();
    }
}
