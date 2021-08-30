// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern crate rusty_v8;

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