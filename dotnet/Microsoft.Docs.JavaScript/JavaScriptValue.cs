using System;
using System.ComponentModel;

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

    public unsafe readonly struct JavaScriptValue
    {
        private const int MaxStackAllocArgs = 10;

        private readonly IntPtr _scope;
        private readonly IntPtr _value;

        internal JavaScriptValue(IntPtr scope, IntPtr value)
        {
            _scope = scope;
            _value = value;
        }

        public JavaScriptValueType Type => js_value_type(_value);

        public string AsString() => FromJsString(_scope, _value);

        public long AsInteger() => js_integer_value(_scope, _value);

        public double AsNumber() => js_number_value(_scope, _value);

        public int ArrayLength() => (int)js_array_length(_value);

        public JavaScriptValue this[string key]
        {
            get => new(_scope, js_object_get_property(_scope, _value, ToJsString(_scope, key)));
            set => js_object_set_property(_scope, _value, ToJsString(_scope, key), value._value);
        }

        public JavaScriptValue this[int index]
        {
            get => new(_scope, js_array_get_index(_scope, _value, (uint)index));
            set => js_array_set_index(_scope, _value, (uint)index, value._value);
        }

        public ObjectEnumerator EnumerateObject() => new ObjectEnumerator(_scope, _value);

        public ArrayEnumerator EnumerateArray() => new ArrayEnumerator(_scope, _value);

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptValue self, params JavaScriptValue[] args)
        {
            return CallFunction(self, args.AsSpan());
        }

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptValue self, ReadOnlySpan<JavaScriptValue> args)
        {
            Span<IntPtr> argv = args.Length <= MaxStackAllocArgs
                ? stackalloc IntPtr[MaxStackAllocArgs]
                : new IntPtr[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                argv[i] = args[i]._value;
            }

            return CallFunction(self, argv, args.Length);
        }

        public (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptValue self, JavaScriptValue arg0)
        {
            Span<IntPtr> args = stackalloc IntPtr[1];
            args[0] = arg0._value;
            return CallFunction(self, args, 1);
        }

        private (JavaScriptValue error, JavaScriptValue result) CallFunction(JavaScriptValue self, ReadOnlySpan<IntPtr> args, nint argc)
        {
            JavaScriptValue error = default;
            JavaScriptValue result = default;

            var scope = _scope;

            fixed (IntPtr* argv = args)
            {
                js_function_call(
                    _scope,
                    _value,
                    self._scope,
                    argv,
                    argc,
                    (scope, value) => error = new(scope, value),
                    (scope, value) => result = new(scope, value));
            }

            return (error, result);
        }

        public ref struct ObjectEnumerator
        {
            private readonly IntPtr _scope;
            private readonly IntPtr _value;
            private readonly IntPtr _propertyNames;
            private readonly uint _length;

            private int _index;

            internal ObjectEnumerator(IntPtr scope, IntPtr value)
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
            private readonly IntPtr _scope;
            private readonly IntPtr _value;
            private readonly uint _length;

            private int _index;

            internal ArrayEnumerator(IntPtr scope, IntPtr value)
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
                    return new(_scope, js_array_get_index(_scope, _value, (uint)_index));
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

    public unsafe ref struct JavaScriptProperty
    {
        private readonly IntPtr _scope;
        private readonly IntPtr _propertyName;
        private readonly IntPtr _propertyValue;

        public JavaScriptProperty(IntPtr scope, IntPtr propertyName, IntPtr propertyValue)
        {
            _scope = scope;
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string key, out JavaScriptValue value)
        {
            key = FromJsString(_scope, _propertyName);
            value = new JavaScriptValue(_scope, _propertyValue);
        }
    }
}
