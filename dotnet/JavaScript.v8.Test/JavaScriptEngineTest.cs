using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JavaScript.v8;

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
        var actualOutput = _js.Run((scope, global) => ToJToken(scope, scope.RunScript(code, "test.js")));

        Assert.Equal(output.Replace('\'', '\"'), actualOutput.ToString(Formatting.None));
    }

    [Theory]
    [InlineData("this is buggy", "SyntaxError: Unexpected identifier")]
    [InlineData("foo()", "ReferenceError: foo is not defined\n    at test.js:1:1")]
    public void RunJavaScript_Error(string code, string error)
    {
        var exception = Assert.Throws<JavaScriptException>(() =>
            _js.Run((scope, global) => scope.RunScript(code, "test.js")));

        Assert.Equal(error, exception.Message);
    }

    [Fact]
    public void RunJavaScript_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _js.Run((scope, global) => throw new InvalidOperationException()));
    }

    [Theory]
    [InlineData("function() { return 3 }", "null", "3")]
    [InlineData("function(a) { return a }", "[1,true,false,0.8482,'a string',{'a':{'b':{}}}]", "[1,true,false,0.8482,'a string',{'a':{'b':{}}}]")]
    public void RunJavaScript_Call_Function_Succeed(string code, string input, string output)
    {
        var actualOutput = _js.Run((scope, global) => ToJToken(
            scope,
            scope.RunScript($"(function() {{ return {code} }})()", "test.js")
                 .CallFunction(scope, scope.CreateUndefined(), ToJavaScriptValue(scope, JToken.Parse(input.Replace('\'', '\"'))))));

        Assert.Equal(output.Replace('\'', '\"'), actualOutput.ToString(Formatting.None));
    }

    [Fact]
    public void RunJavaScript_SetGlobal_Succeed()
    {
        var actualOutput = _js.Run((scope, global) =>
        {
            Assert.Equal(JavaScriptValueType.Object, global.Type);
            global.ObjectSetProperty(scope, "foo", scope.CreateString("bar"));

            return scope.RunScript("foo", "test.js").AsString(scope);
        });

        Assert.Equal("bar", actualOutput);
    }

    [Fact]
    public void RunJavaScript_CustomFunction_Succeed()
    {
        var actualOutput = _js.Run((scope, global) =>
        {
            var foo = scope.CreateFunction((scope, self, args) =>
            {
                var result = ToJToken(scope, self).ToString(Formatting.None);
                foreach (var arg in args)
                {
                    result += "," + ToJToken(scope, arg).ToString(Formatting.None);
                }
                return scope.CreateString(result);
            });

            return scope.RunScript("(function () { return function(foo) { return foo(true,false,'a',0) } })()", "test.js")
                 .CallFunction(scope, scope.CreateUndefined(), foo)
                 .AsString(scope);
        });

        Assert.Equal("{},true,false,\"a\",0", actualOutput);
    }

    [Fact]
    public void RunJavaScript_CustomFunction_ThrowsException()
    {
        var exception = Assert.Throws<JavaScriptException>(() => _js.Run((scope, global) =>
        {
            var foo = scope.CreateFunction((scope, self, args) => throw new Exception("my exception"));

            global.ObjectSetProperty(scope, "foo", foo);

            scope.RunScript("foo()", "test.js");
        }));

        Assert.Contains("my exception", exception.Message);
    }

    private static JavaScriptValue ToJavaScriptValue(JavaScriptScope scope, JToken value)
    {
        switch (value.Type)
        {
            case JTokenType.Null:
                return scope.CreateNull();
            case JTokenType.Undefined:
                return scope.CreateUndefined();
            case JTokenType.String:
                return scope.CreateString(value.ToString());
            case JTokenType.Integer:
                return scope.CreateInteger((int)value);
            case JTokenType.Float:
                return scope.CreateNumber((double)value);
            case JTokenType.Boolean:
                return (bool)value ? scope.CreateTrue() : scope.CreateFalse();
            case JTokenType.Array when value is JArray jArray:
                var array = scope.CreateArray(jArray.Count);
                for (var i = 0; i < jArray.Count; i++)
                {
                    array.ArraySetIndex(scope, i, ToJavaScriptValue(scope, jArray[i]));
                }
                return array;
            case JTokenType.Object when value is JObject jObj:
                var obj = scope.CreateObject();
                foreach (var (key, item) in jObj)
                {
                    obj.ObjectSetProperty(scope, key, ToJavaScriptValue(scope, item));
                }
                return obj;
            default:
                throw new NotSupportedException();
        }
    }

    private static JToken ToJToken(JavaScriptScope scope, JavaScriptValue value)
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
                return new JValue(value.AsString(scope));
            case JavaScriptValueType.Integer:
                return new JValue(value.AsInteger(scope));
            case JavaScriptValueType.Number:
                return new JValue(value.AsNumber(scope));
            case JavaScriptValueType.Array:
                var array = new JArray();
                foreach (var item in value.EnumerateArray(scope))
                {
                    array.Add(ToJToken(scope, item));
                }
                return array;
            case JavaScriptValueType.Object:
                var obj = new JObject();
                foreach (var (key, item) in value.EnumerateObject(scope))
                {
                    obj.Add(key, ToJToken(scope, item));
                }
                return obj;
            default:
                throw new NotSupportedException();
        }
    }
}
