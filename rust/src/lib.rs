// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern crate rusty_v8;

use rusty_v8 as v8;
use std::sync::Once;

static V8_INITIALIZE: Once = Once::new();

type JsRun = extern "C" fn(scope: &mut v8::HandleScope, global: v8::Local<v8::Object>);
type JsResult = extern "C" fn(scope: &mut v8::HandleScope, value: v8::Local<v8::Value>);
type JsFunction = extern "C" fn(
    scope: &mut v8::HandleScope,
    this: v8::Local<v8::Object>,
    argv: *const v8::Local<v8::Value>,
    argc: i32,
) -> v8::Local<'static, v8::Value>;

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
pub extern "C" fn js_value_type(value: v8::Local<v8::Value>) -> JsValueType {
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
    } else if value.is_function() {
        return JsValueType::Function;
    } else if value.is_array() {
        return JsValueType::Array;
    } else if value.is_object() {
        return JsValueType::Object;
    } else {
        return JsValueType::Unknown;
    }
}

#[no_mangle]
pub extern "C" fn js_undefined<'a>(
    scope: &mut v8::HandleScope<'a>,
) -> v8::Local<'a, v8::Primitive> {
    v8::undefined(scope)
}

#[no_mangle]
pub extern "C" fn js_null<'a>(scope: &mut v8::HandleScope<'a>) -> v8::Local<'a, v8::Primitive> {
    v8::null(scope)
}

#[no_mangle]
pub extern "C" fn js_true<'a>(scope: &mut v8::HandleScope<'a>) -> v8::Local<'a, v8::Boolean> {
    v8::Boolean::new(scope, true)
}

#[no_mangle]
pub extern "C" fn js_false<'a>(scope: &mut v8::HandleScope<'a>) -> v8::Local<'a, v8::Boolean> {
    v8::Boolean::new(scope, false)
}

#[no_mangle]
pub extern "C" fn js_integer_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: i32,
) -> v8::Local<'a, v8::Integer> {
    v8::Integer::new(scope, value)
}

#[no_mangle]
pub extern "C" fn js_integer_value<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Value>,
) -> i64 {
    return value.integer_value(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_number_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: f64,
) -> v8::Local<'a, v8::Number> {
    v8::Number::new(scope, value)
}

#[no_mangle]
pub extern "C" fn js_number_value<'a>(
    scope: &mut v8::HandleScope<'a>,
    value: v8::Local<'a, v8::Value>,
) -> f64 {
    return value.number_value(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_string_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    chars: *const u16,
    length: usize,
) -> v8::Local<'a, v8::String> {
    let string = unsafe { std::slice::from_raw_parts(chars, length) };
    return v8::String::new_from_two_byte(scope, string, v8::NewStringType::Normal).unwrap();
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
    length: usize,
) {
    let buffer = unsafe { std::slice::from_raw_parts_mut(buffer, length) };
    value.write(scope, buffer, 0, v8::WriteOptions::NO_OPTIONS);
}

#[no_mangle]
pub extern "C" fn js_array_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    length: i32,
) -> v8::Local<'a, v8::Array> {
    v8::Array::new(scope, length)
}

#[no_mangle]
pub extern "C" fn js_array_length<'a>(array: v8::Local<'a, v8::Array>) -> u32 {
    return array.length();
}

#[no_mangle]
pub extern "C" fn js_array_get_index<'a>(
    scope: &mut v8::HandleScope<'a>,
    array: v8::Local<'a, v8::Array>,
    index: u32,
) -> v8::Local<'a, v8::Value> {
    return array.get_index(scope, index).unwrap();
}

#[no_mangle]
pub extern "C" fn js_array_set_index<'a>(
    scope: &mut v8::HandleScope<'a>,
    array: v8::Local<'a, v8::Array>,
    index: u32,
    value: v8::Local<'a, v8::Value>,
) {
    array.set_index(scope, index, value).unwrap();
}

#[no_mangle]
pub extern "C" fn js_object_new<'a>(scope: &mut v8::HandleScope<'a>) -> v8::Local<'a, v8::Object> {
    v8::Object::new(scope)
}

#[no_mangle]
pub extern "C" fn js_object_get_own_property_names<'a>(
    scope: &mut v8::HandleScope<'a>,
    obj: v8::Local<'a, v8::Object>,
) -> v8::Local<'a, v8::Array> {
    return obj.get_own_property_names(scope).unwrap();
}

#[no_mangle]
pub extern "C" fn js_object_get_property<'a>(
    scope: &mut v8::HandleScope<'a>,
    obj: v8::Local<'a, v8::Object>,
    key: v8::Local<'a, v8::Value>,
) -> v8::Local<'a, v8::Value> {
    return obj.get(scope, key).unwrap();
}

#[no_mangle]
pub extern "C" fn js_object_set_property<'a>(
    scope: &mut v8::HandleScope<'a>,
    obj: v8::Local<'a, v8::Object>,
    key: v8::Local<'a, v8::Value>,
    value: v8::Local<'a, v8::Value>,
) {
    obj.set(scope, key, value).unwrap();
}

#[no_mangle]
pub extern "C" fn js_function_new<'a>(
    scope: &mut v8::HandleScope<'a>,
    callback: JsFunction,
) -> v8::Local<'a, v8::Function> {
    let callback = v8::External::new(scope, callback as *mut std::ffi::c_void);
    v8::Function::builder(js_function_callback)
        .data(callback.into())
        .build(scope)
        .unwrap()
}

fn js_function_callback(
    scope: &mut v8::HandleScope,
    args: v8::FunctionCallbackArguments,
    mut retval: v8::ReturnValue,
) {
    let callback = unsafe { v8::Local::<v8::External>::cast(args.data().unwrap()) };
    let callback: JsFunction = unsafe { std::mem::transmute(callback.value()) };
    let argc = args.length();
    let mut argv = Vec::with_capacity(argc as usize);
    for i in 0..argc {
        argv.push(args.get(i));
    }
    retval.set((callback)(scope, args.this(), argv.as_ptr(), argc));
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
    let args = unsafe { std::slice::from_raw_parts(argv, argc) };
    let scope = &mut v8::TryCatch::new(scope);

    match value.call(scope, recv, args) {
        Some(value) => (result)(scope, value),
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

#[no_mangle]
pub extern "C" fn js_run_in_context(isolate: *mut v8::OwnedIsolate, callback: JsRun) {
    let isolate = unsafe { isolate.as_mut().unwrap() };
    let scope = &mut v8::HandleScope::new(isolate);
    let context = v8::Context::new(scope);
    let scope = &mut v8::ContextScope::new(scope, context);
    let mut scope = &mut v8::HandleScope::new(scope);
    let global = context.global(&mut scope);

    callback(&mut scope, global);
}

#[no_mangle]
pub extern "C" fn js_run_script<'a>(
    scope: &mut v8::HandleScope<'a>,
    code: v8::Local<'a, v8::String>,
    filename: v8::Local<'a, v8::String>,
    error: JsResult,
    result: JsResult,
) {
    let undefined = v8::undefined(scope);
    let origin = v8::ScriptOrigin::new(
        scope,
        filename.into(),
        0,
        0,
        false,
        0,
        undefined.into(),
        false,
        false,
        false,
    );
    let scope = &mut v8::TryCatch::new(scope);

    match v8::Script::compile(scope, code, Some(&origin)) {
        None => report_errors(scope, error),
        Some(script) => match script.run(scope) {
            Some(value) => (result)(scope, value),
            None => report_errors(scope, error),
        },
    };
}

fn report_errors(try_catch: &mut v8::TryCatch<v8::HandleScope>, error: JsResult) {
    let message = try_catch.stack_trace().unwrap();
    (error)(try_catch, message);
}
