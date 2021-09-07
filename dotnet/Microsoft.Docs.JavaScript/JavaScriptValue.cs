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

    public unsafe ref struct JavaScriptValue
    {
        private readonly IntPtr _scope;
        private readonly IntPtr _value;

        internal JavaScriptValue(IntPtr scope, IntPtr value)
        {
            _scope = scope;
            _value = value;
        }

        public JavaScriptValueType GetValueType() => js_value_type(_value);

        public string AsString() => FromJsString(_scope, _value);

        public long AsInteger() => js_value_as_integer(_scope, _value);

        public double AsNumber() => js_value_as_number(_scope, _value);

        public ObjectEnumerator EnumerateObject() => new ObjectEnumerator(_scope, _value);

        public ArrayEnumerator EnumerateArray() => new ArrayEnumerator(_scope, _value);

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
