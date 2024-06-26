// --------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the ""Software""), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
// --------------------------------------------------------------------------

import Foundation

// Using @available to rename a protocol

public protocol MyRenamedProtocol {
    // A protocol named MyProtocol was subsequent release renames MyProtocol
}

@available(*, unavailable, renamed: "MyRenamedProtocol")
public typealias MyProtocol = MyRenamedProtocol

// Using @available to specify OS versions

@available(iOS 10.0, macOS 10.12, *)
public class MyClass {
    // class definition
}

// Using @available to specify swift version

@available(swift 3.0.2)
@available(macOS 10.12, *)
public struct MyStruct {
    // struct definition
}

// Using @objc

public class ExampleClass: NSObject {
    @objc public var enabled: Bool {
        return true
    }
}

// Use of @unchecked Sendable

public class SomeSendable: @unchecked Sendable {
    public let name: String

    public init(name: String) {
        self.name = name
    }
}
