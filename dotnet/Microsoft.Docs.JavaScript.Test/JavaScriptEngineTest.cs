using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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
        [InlineData("[1,3.14,true,false]", "[1,3.14,true,false]")]
        [InlineData("[{a:0,'b c':{d:[]}}]", "[{'a':0,'b c':{'d':[]}}]")]
        public void RunJavaScript_Succeed(string code, string output)
        {
            var hasError = false;
            var actualOutput = (JToken)JValue.CreateUndefined();

            _js.Run(scope => scope.RunScript(
                code,
                "test.js",
                (scope, error) => hasError = true,
                (scope, value) => actualOutput = ToJToken(value)));

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
                (scope, error) => actualError = ToJToken(error),
                (scope, value) => { }));

            Assert.Contains(error, actualError);
        }

        [Theory]
        [InlineData("function() { return 3 }", "null", "3")]
        [InlineData("function(a) { return a }", "[1,true,false,0.8482,'a string',{'a':{'b':{}}}]", "[1,true,false,0.8482,'a string',{'a':{'b':{}}}]")]
        public void RunJavaScript_Call_Function_Succeed(string code, string input, string output)
        {
            var hasError = false;
            var actualOutput = (JToken)JValue.CreateUndefined();

            _js.Run(scope =>
            {
                scope.RunScript(
                    $"(function() {{ return {code} }})()",
                    "test.js",
                    (scope, error) => hasError = true,
                    (scope, value) =>
                    {
                        Assert.Equal(JavaScriptValueType.Function, value.Type);
                        actualOutput = ToJToken(
                            value.CallFunction(
                                scope.CreateUndefined(),
                                ToJavaScriptValue(scope, JToken.Parse(input.Replace('\'', '\"')))).result);
                    });
            });

            Assert.False(hasError);
            Assert.Equal(output.Replace('\'', '\"'), actualOutput.ToString(Formatting.None));
        }

        [Theory]
        [InlineData("function(foo) { return foo(true,false,'a',0) }", "{},true,false,'a',0")]
        public void RunJavaScript_UserFunction_Succeed(string code, string output)
        {
            var hasError = false;
            var actualOutput = "";

            _js.Run(scope =>
            {
                var foo = scope.CreateFunction((scope, self, args) => scope.CreateString(
                    ToJToken(self) + "," +
                    string.Join(",", args.ToArray().Select(arg => ToJToken(arg).ToString(Formatting.None)))));

                scope.RunScript(
                    $"(function() {{ return {code} }})()",
                    "test.js",
                    (scope, error) => hasError = true,
                    (scope, value) =>
                    {
                        Assert.Equal(JavaScriptValueType.Function, value.Type);
                        actualOutput = value.CallFunction(scope.CreateUndefined(), foo).result.AsString();
                    });
            });

            Assert.False(hasError);
            Assert.Equal(output.Replace('\'', '\"'), actualOutput);
        }

        private static JavaScriptValue ToJavaScriptValue(JavaScriptScope context, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Null:
                    return context.CreateNull();
                case JTokenType.Undefined:
                    return context.CreateUndefined();
                case JTokenType.String:
                    return context.CreateString(value.ToString());
                case JTokenType.Integer:
                    return context.CreateInteger((int)value);
                case JTokenType.Float:
                    return context.CreateNumber((double)value);
                case JTokenType.Boolean:
                    return (bool)value ? context.CreateTrue() : context.CreateFalse();
                case JTokenType.Array when value is JArray jArray:
                    var array = context.CreateArray(jArray.Count);
                    for (var i = 0; i < jArray.Count; i++)
                    {
                        array[i] = ToJavaScriptValue(context, jArray[i]);
                    }
                    return array;
                case JTokenType.Object when value is JObject jObj:
                    var obj = context.CreateObject();
                    foreach (var (key, item) in jObj)
                    {
                        obj[key] = ToJavaScriptValue(context, item);
                    }
                    return obj;
                default:
                    throw new NotSupportedException();
            }
        }

        private static JToken ToJToken(JavaScriptValue value)
        {
            switch (value.Type)
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
