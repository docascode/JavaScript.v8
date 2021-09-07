using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        [InlineData("undefined", "undefined")]
        [InlineData("'hello' + ' world'", "'hello world'")]
        [InlineData("[1,3.14]", "[1,3.14]")]
        public void RunJavaScript_Succeed(string code, string output)
        {
            var hasError = false;
            var actualOutput = (JToken)JValue.CreateUndefined();

            _js.Run(scope => scope.RunScript(
                code,
                "test.js",
                error => hasError = true,
                value => actualOutput = ToJToken(value)));

            Assert.False(hasError);
            Assert.Equal(output.Replace('\'', '\"'), actualOutput.ToString(Formatting.None));
        }

        [Theory]
        [InlineData("this is buggy", "SyntaxError: Unexpected identifier")]
        public void RunJavaScript_Compile_Error(string code, string error)
        {
            var actualError = (JToken)JValue.CreateUndefined();

            _js.Run(scope => scope.RunScript(
                code,
                "test.js",
                error => actualError = ToJToken(error),
                value => { }));

            Assert.Contains(error, actualError);
        }

        private static JToken ToJToken(JavaScriptValue value)
        {
            switch (value.GetValueType())
            {
                case JavaScriptValueType.Null:
                    return JValue.CreateNull();
                case JavaScriptValueType.Undefined:
                    return JValue.CreateUndefined();
                case JavaScriptValueType.True:
                    return new JValue(true);
                case JavaScriptValueType.False:
                    return new JValue(false);
                case JavaScriptValueType.String:
                    return new JValue(value.AsString());
                case JavaScriptValueType.Integer:
                    return new JValue(value.AsInteger());
                case JavaScriptValueType.Number:
                    return new JValue(value.AsNumber());
                case JavaScriptValueType.Array:
                    var array = new JArray();
                    foreach (var item in value.EnumerateArray())
                    {
                        array.Add(ToJToken(item));
                    }
                    return array;
                case JavaScriptValueType.Object:
                    var obj = new JObject();
                    foreach (var (key, item) in value.EnumerateObject())
                    {
                        obj.Add(key, ToJToken(item));
                    }
                    return obj;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
