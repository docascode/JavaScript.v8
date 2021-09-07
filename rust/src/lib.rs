// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern crate rusty_v8;

use rusty_v8 as v8;
use std::sync::Once;

static V8_INITIALIZE: Once = Once::new();

#[repr(C)]
pub enum JsValueType {
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

#[no_mangle]
pub extern "C" fn js_value_type(value: v8::Local<v8::Value>) ->JsValueType {
    if value.is_undefined() {
        return JsValueType::Undefined;
    } else if value.is_null() {
        return JsValueType::Null;
    } else if value.is_true() {
        return JsValueType::True;
    } else if value.is_false() {
        return JsValueType::False;
    } else if value.is_int32() {
        return JsValueType::Integer;
    } else if value.is_number() {
        return JsValueType::Number;
    } else if value.is_string() {
        return JsValueType::String;
    } else if value.is_array() {
        return JsValueType::Array;
    } else if value.is_object() {
        return JsValueType::Object;
    } else if value.is_function() {
        return JsValueType::Function;
    } else {
        return JsValueType::Unknown;
    }
}

#[no_mangle]
pub extern "C" fn js_value_as_integer<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Value>
) -> i64 {
    return value.integer_value(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_value_as_number<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Value>
) -> f64 {
    return value.number_value(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_object_get_own_property_names<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Object>
) -> v8::Local<'a, v8::Array> {
    return value.get_own_property_names(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_object_get_property<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Object>,
    key: v8::Local<'a, v8::Value>,
) -> v8::Local<'a, v8::Value> {
    return value.get(scope, key).unwrap();
}

#[no_mangle]
pub extern "C" fn js_array_length<'a>(
    value: v8::Local<'a, v8::Array>
) -> u32 {
    return value.length();
}

#[no_mangle]
pub extern "C" fn js_array_get_index<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Array>,
    index: u32,
) -> v8::Local<'a, v8::Value> {
    return value.get_index(scope, index).unwrap();
}

#[no_mangle]
pub extern "C" fn js_string_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    chars: *const u16,
    length: usize
) -> v8::Local<'a, v8::String> {
    let string = unsafe { std::slice::from_raw_parts(chars, length) };
    let string = v8::String::new_from_two_byte(scope, string, v8::NewStringType::Normal).unwrap();
    string
}

#[no_mangle]
pub extern "C" fn js_string_length<'a>(value: v8::Local<'a, v8::String>) -> usize {
    value.length()
}

#[no_mangle]
pub extern "C" fn js_string_copy<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::String>,
    buffer: *mut u16,
    length: usize
) {
    let buffer = unsafe { std::slice::from_raw_parts_mut(buffer, length) };
    value.write(scope, buffer, 0, v8::WriteOptions::NO_OPTIONS);
}

#[no_mangle]
pub extern "C" fn js_function_call<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Function>,
    recv: v8::Local<'a, v8::Value>,
    argv: *const v8::Local<'a, v8::Value>,
    argc: usize,
    error: JsResult,
    result: JsResult,
) {
    let mut scope = v8::TryCatch::new(scope);
    let args = unsafe { std::slice::from_raw_parts(argv, argc) };
    match value.call(&mut scope, recv, args) {
        Some(value) => (result)(&mut scope, value),
        None => report_errors(scope, error),
    }
}

#[no_mangle]
pub extern "C" fn js_isolate_new() -> *mut v8::OwnedIsolate {
    V8_INITIALIZE.call_once(|| {
        let platform = v8::new_default_platform(0, false).make_shared();
        v8::V8::initialize_platform(platform);
        v8::V8::initialize();
    });

    let isolate = v8::Isolate::new(Default::default());
    let isolate = Box::new(isolate);
    Box::into_raw(isolate)
}

#[no_mangle]
pub extern "C" fn js_isolate_delete(isolate: *mut v8::OwnedIsolate) {
    unsafe { Box::from_raw(isolate) };
}

type JsRun = extern fn(scope: &mut v8::HandleScope);

#[no_mangle]
pub extern "C" fn js_run_in_context(isolate: *mut v8::OwnedIsolate, callback: JsRun) {
    let isolate = unsafe { isolate.as_mut().unwrap() };
    let handle_scope = &mut v8::HandleScope::new(isolate);
    let context = v8::Context::new(handle_scope);
    let context_scope = &mut v8::ContextScope::new(handle_scope, context);
    let scope = &mut v8::HandleScope::new(context_scope);

    callback(scope)
}

type JsResult = extern fn(scope: &mut v8::HandleScope, value: v8::Local<v8::Value>);

#[no_mangle]
pub extern "C" fn js_run_script<'a>(
    scope: &mut v8::HandleScope<'a>,
    code: v8::Local<'a, v8::String>,
    filename: v8::Local<'a, v8::String>,
    error: JsResult,
    result: JsResult,
) {
    let mut scope = v8::TryCatch::new(scope);
    let undefined = v8::undefined(&mut scope);
    let origin = v8::ScriptOrigin::new(&mut scope, filename.into(), 0, 0, false, 0, undefined.into(), false, false, false);

    match v8::Script::compile(&mut scope, code, Some(&origin)) {
        None => report_errors(scope, error),
        Some(script) => match script.run(&mut scope) {
            Some(value) => (result)(&mut scope, value),
            None => report_errors(scope, error),
        }
    };
}

fn report_errors(mut try_catch: v8::TryCatch<v8::HandleScope>, error: JsResult) {
    let exception = try_catch.exception().unwrap();
    (error)(&mut try_catch, exception)
}

#[test]
fn test_run_javascript() {
    assert_eq!(8, std::mem::size_of::<&mut v8::HandleScope>());
    assert_eq!(8, std::mem::size_of::<v8::Local<v8::String>>());
}
