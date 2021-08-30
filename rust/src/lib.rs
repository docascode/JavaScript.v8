// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern crate rusty_v8;

use rusty_v8 as v8;
use std::sync::Once;

static V8_INITIALIZE: Once = Once::new();

pub struct JsIsolate(v8::OwnedIsolate);

#[repr(C)]
pub enum JsonValueKind {
    Undefined,
    Object,
    Array,
    String,
    Number,
    True,
    False,
    Null,
}

#[repr(C)]
pub struct JsonReader {

}

type JsonWriteNone = fn();
type JsonWriteInt = fn(i64);
type JsonWriteNumber = fn(f64);
type JsonWriteString = fn(usize) -> *mut u16;

#[repr(C)]
pub struct JsonWriter {
    write_undefined: JsonWriteNone,
    write_null: JsonWriteNone,
    write_true: JsonWriteNone,
    write_false: JsonWriteNone,
    write_array: JsonWriteInt,
    write_int: JsonWriteInt,
    write_number: JsonWriteNumber,
    write_string: JsonWriteString,
}

#[no_mangle]
pub extern "C" fn js_new() -> *mut v8::OwnedIsolate {
    V8_INITIALIZE.call_once(|| {
        let platform = v8::new_default_platform(0, false).make_shared();
        v8::V8::initialize_platform(platform);
        v8::V8::initialize();
    });

    &mut v8::Isolate::new(Default::default())
}

#[no_mangle]
pub extern "C" fn js_run(isolate: *mut v8::OwnedIsolate, code: *mut u16, length: usize, writer: *mut JsonWriter) -> i32 {
    let isolate = unsafe { isolate.as_mut().unwrap() };
    let code = unsafe { std::slice::from_raw_parts(code, length) };
    let writer = unsafe { writer.as_mut().unwrap() };

    let scope = &mut v8::HandleScope::new(isolate);
    let context = v8::Context::new(scope);
    let scope = &mut v8::ContextScope::new(scope, context);

    let code = v8::String::new_from_two_byte(scope, code, v8::NewStringType::Normal).unwrap();
    let script = v8::Script::compile(scope, code, None).unwrap();
    let result = script.run(scope).unwrap();
    write_json(scope, writer, result);

    0
}

fn write_json(scope: &mut v8::HandleScope, writer: &mut JsonWriter, value: v8::Local<v8::Value>) {
    if value.is_undefined() {
        (writer.write_undefined)();
    } else if value.is_null() {
        (writer.write_null)();
    } else if value.is_true() {
        (writer.write_true)();
    } else if value.is_false() {
        (writer.write_false)();
    } else if value.is_int32() {
        (writer.write_int)(value.to_int32(scope).unwrap().value());
    } else if value.is_number() {
        (writer.write_number)(value.to_number(scope).unwrap().value());
    } else if value.is_string() {
        let value = value.to_string(scope).unwrap();
        let length = value.write(scope, null, 0, -1, v8::WriteOptions::NO_OPTIONS);
        let buffer = (writer.write_string)(value.length());
        value.write(scope, buffer, 0, length, v8::WriteOptions::NO_OPTIONS);
    }
}

pub fn a() {
    let isolate = &mut v8::Isolate::new(Default::default());

    let scope = &mut v8::HandleScope::new(isolate);
    let context = v8::Context::new(scope);
    let scope = &mut v8::ContextScope::new(scope, context);

    let code = v8::String::new(scope, "'Hello' + ' World!'").unwrap();
    println!("javascript code: {}", code.to_rust_string_lossy(scope));

    let script = v8::Script::compile(scope, code, None).unwrap();
    let result = script.run(scope).unwrap();
    let result = result.to_number(scope).unwrap();
    println!("result: {}", result.to_rust_string_lossy(scope));
}
