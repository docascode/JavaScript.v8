using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using static Microsoft.Docs.Build.NativeMethods;

namespace Microsoft.Docs.Build
{
    public enum JavaScriptValueType
    {
        Unknown,
        Undefined,
        Object,
        Array,
        String,
        Integer,
        Number,
        True,
        False,
        Null,
        Function,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly struct JavaScriptValue
    {
        private readonly IntPtr _;

        public JavaScriptValueType Type => js_value_type(this);

        public string AsString(JavaScriptScope scope) => FromJsString(scope, this);

        public long AsInteger(JavaScriptScope scope) => js_integer_value(scope, this);

        public double AsNumber(JavaScriptScope scope) => js_number_value(scope, this);

        public int ArrayLength() => (int)js_array_length(this);

        public JavaScriptValue ArrayGetIndex(JavaScriptScope scope, int index) => js_array_get_index(scope, this, (uint)index);

        public void ArraySetIndex(JavaScriptScope scope, int index, JavaScriptValue value) => js_array_set_index(scope, this, (uint)index, value);

        public JavaScriptValue ObjectGetProperty(JavaScriptScope scope, string key) => js_object_get_property(scope, this, ToJsString(scope, key));

        public void ObjectSetProperty(JavaScriptScope scope, string key, JavaScriptValue value) => js_object_set_property(scope, this, ToJsString(scope, key), value);

        public ObjectEnumerator EnumerateObject(JavaScriptScope scope) => new ObjectEnumerator(scope, this);

        public ArrayEnumerator EnumerateArray(JavaScriptScope scope) => new ArrayEnumerator(scope, this);

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptScope scope, JavaScriptValue self, params JavaScriptValue[] args)
        {
            fixed (JavaScriptValue* argv = args)
            {
                return CallFunction(scope, self, argv, args.Length);
            }
        }

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptScope scope, JavaScriptValue self, ReadOnlySpan<JavaScriptValue> args)
        {
            fixed (JavaScriptValue* argv = args)
            {
                return CallFunction(scope, self, argv, args.Length);
            }
        }

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptScope scope, JavaScriptValue self, JavaScriptValue arg0)
        {
            return CallFunction(scope, self, &arg0, 1);
        }

        private (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptScope scope, JavaScriptValue self, JavaScriptValue* argv, int argc)
        {
            JavaScriptValue error = default;
            JavaScriptValue result = default;

            js_function_call(scope, this, self, argv, argc, (scope, value) => error = value, (scope, value) => result = value);

            return (error, result);
        }

        public ref struct ObjectEnumerator
        {
            private readonly JavaScriptScope _scope;
            private readonly JavaScriptValue _value;
            private readonly JavaScriptValue _propertyNames;
            private readonly uint _length;

            private int _index;

            internal ObjectEnumerator(JavaScriptScope scope, JavaScriptValue value)
            {
                _index = -1;
                _scope = scope;
                _value = value;
                _propertyNames = js_object_get_own_property_names(_scope, _value);
                _length = js_array_length(_propertyNames);
            }

            public JavaScriptProperty Current
            {
                get
                {
                    var propertyName = js_array_get_index(_scope, _propertyNames, (uint)_index);
                    var propertyValue = js_object_get_property(_scope, _value, propertyName);

                    return new(_scope, propertyName, propertyValue);
                }
            }

            public ObjectEnumerator GetEnumerator()
            {
                var result = this;
                result._index = -1;
                return result;
            }

            public bool MoveNext()
            {
                return ++_index < _length;
            }
        }

        public ref struct ArrayEnumerator
        {
            private readonly JavaScriptScope _scope;
            private readonly JavaScriptValue _value;
            private readonly uint _length;

            private int _index;

            internal ArrayEnumerator(JavaScriptScope scope, JavaScriptValue value)
            {
                _index = -1;
                _scope = scope;
                _value = value;
                _length = js_array_length(_value);
            }

            public JavaScriptValue Current
            {
                get
                {
                    return js_array_get_index(_scope, _value, (uint)_index);
                }
            }

            public ArrayEnumerator GetEnumerator()
            {
                var result = this;
                result._index = -1;
                return result;
            }

            public bool MoveNext()
            {
                return ++_index < _length;
            }
        }
    }

    public unsafe readonly ref struct JavaScriptProperty
    {
        private readonly JavaScriptScope _scope;
        private readonly JavaScriptValue _propertyName;
        private readonly JavaScriptValue _propertyValue;

        public JavaScriptProperty(JavaScriptScope scope, JavaScriptValue propertyName, JavaScriptValue propertyValue)
        {
            _scope = scope;
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string key, out JavaScriptValue value)
        {
            key = FromJsString(_scope, _propertyName);
            value = _propertyValue;
        }
    }
}
