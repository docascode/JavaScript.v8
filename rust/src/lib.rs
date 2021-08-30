// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern crate rusty_v8;

use rusty_v8 as v8;
use std::sync::Once;

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

type WriteJsonNone = fn();
type WriteJsonInt = fn(i64);
type WriteJsonNumber = fn(f64);
type WriteJsonString = fn(usize) -> *mut u16;

#[repr(C)]
pub struct JsonWriter {
    write_undefined: WriteJsonNone,
    write_null: WriteJsonNone,
    write_true: WriteJsonNone,
    write_false: WriteJsonNone,
    write_array: WriteJsonInt,
    write_int: WriteJsonInt,
    write_number: WriteJsonNumber,
    write_string: WriteJsonString,
    write_start_array: WriteJsonInt,
    write_end_array: WriteJsonNone,
    write_start_object: WriteJsonNone,
    write_property_name: WriteJsonString,
    write_end_object: WriteJsonNone,
}

static V8_INITIALIZE: Once = Once::new();

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
    } else if value.is_number() {
        match value.integer_value(scope) {
            Some(value) => (writer.write_int)(value),
            _ => (writer.write_number)(value.number_value(scope).unwrap()),
        };
    } else if value.is_string() {
        write_json_string(scope, writer.write_string, value);
    } else if value.is_array() {
        let length_str = v8::String::new(scope, "length").unwrap();
        let value = value.to_object(scope).unwrap();
        let length = value.get(scope, length_str.into()).unwrap().integer_value(scope).unwrap();
        (writer.write_start_array)(length);
        for i in 0..length as u32 {
            let item = value.get_index(scope, i).unwrap();
            write_json(scope, writer, item);
        }
        (writer.write_end_array)();
    } else if value.is_object() {
        let value = value.to_object(scope).unwrap();
        let property_names = value.get_own_property_names(scope).unwrap();
        (writer.write_start_object)();
        for i in 0..property_names.length() {
            let property_name = property_names.get_index(scope, i).unwrap();
            let item = value.get(scope, property_name).unwrap();
            write_json_string(scope, writer.write_property_name, item);
        }
        (writer.write_end_object)();
    }
}

fn write_json_string(scope: &mut v8::HandleScope, write: WriteJsonString, value: v8::Local<v8::Value>) {
    let value = value.to_string(scope).unwrap();
    let buffer = write(value.length());
    let buffer = unsafe { std::slice::from_raw_parts_mut(buffer, value.length()) };
    value.write(scope, buffer, 0, v8::WriteOptions::NO_OPTIONS);
}
