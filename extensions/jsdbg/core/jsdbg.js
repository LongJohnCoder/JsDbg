"use strict";

// jsdbg.js
// Core jsdbg interfaces.

var JsDbg = undefined;
Loader.OnLoad(function () {
    var debuggerBrokeListeners = [];
    var memoryWriteListeners = [];

    function esc(s) { return encodeURIComponent(s); }
    function fireListeners(listeners) {
        listeners.forEach(function (f) {
            try {
                f();
            } finally {

            }
        })
    }

    var sizeNames = {
        1 : "sbyte",
        2 : "short",
        4 : "int",
        8 : "long"
    };
    var unsignedNames = {
        1 : "byte",
        2 : "ushort",
        4 : "uint",
        8 : "ulong"
    };
    var floatSizeNames = {
        4 : "float",
        8 : "double"
    };

    function getSizeName(size, isUnsigned, isFloat) {
        var sizeIndex = isFloat ? floatSizeNames : isUnsigned ? unsignedNames : sizeNames;
        if (size in sizeIndex) {
            return sizeIndex[size];
        } else {
            console.error("Invalid number size:" + size + " unsigned:" + isUnsigned + " float:" + isFloat);
            return null;
        }
    }

    function readJsonFloat(val) {
        if (val === "Infinity") {
            return Infinity;
        } else if (val === "-Infinity") {
            return -Infinity;
        } else if (val === "NaN") {
            return NaN;
        } else {
            return val;
        }
    }

    JsDbgTransport.OnOutOfBandMessage(function (message) {
        if (message == "break") {
            JsDbgLoadingIndicator.SetIsWaitingForDebugger(false);
            JsDbgTransport.InvalidateCache();
            fireListeners(debuggerBrokeListeners);
            fireListeners(memoryWriteListeners);
        } else if (message == "waiting") {
            JsDbgLoadingIndicator.SetIsWaitingForDebugger(true);
        } else {
            console.error("Unexpected out of band message: " + message);
        }
    })

    JsDbg = {
        _help: {
            name:"JsDbg",
            description: "JsDbg core interfaces.",
            notes: "<p>NOTE: These APIs are designed for minimalism rather than usability; extensions like DbgObject or Catalog should generally be used instead.</p>"
        },

        _help_LoadExtension: {
            description: "Load an extension at a given path.",
            arguments: [
                {name:"path", type:"string", description:"The path of the extension to load.  Relative paths are relative to the extensions directory."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LoadExtension: function(path, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/loadextension?path=" + esc(path), function (result) {
                JsDbgToolbar.UpdateExtensionList();
                callback(result);
            }, JsDbgTransport.CacheType.Uncached);
        },

        _help_UnloadExtension: {
            description: "Unloads an extension identified by name.",
            arguments: [
                {name:"name", type:"string", description:"The name of the extension to unload."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        UnloadExtension: function(name, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/unloadextension?name=" + esc(name), function (result) {
                JsDbgToolbar.UpdateExtensionList();
                callback(result);
            }, JsDbgTransport.CacheType.Uncached);
        },

        _help_GetExtensions: {
            description: "Gets all loaded extensions.",
            arguments: [
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        GetExtensions: function(callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/extensions", callback, JsDbgTransport.CacheType.Uncached);
        },

        _help_GetExtensionPath: {
            description: "Gets the default extension path.",
            arguments: [
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        GetExtensionPath: function(callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/extensionpath", callback, JsDbgTransport.CacheType.Uncached, "GET");
        },

        _help_SetExtensionPath: {
            description: "Sets the default extension path, unloads extensions from the previous path and loads the \"default\" extension from the new path.",
            arguments: [
                {name:"path", type:"string", description:"The new extension path."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        SetExtensionPath: function(path, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/extensionpath", callback, JsDbgTransport.CacheType.Uncached, "PUT", path);
        },

        _help_LookupTypeSize: {
            description: "Looks up the size of the type.",
            arguments: [
                {name:"module", type:"string", description:"The module of the type."},
                {name:"type", type:"string", description:"The type."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupTypeSize: function(module, type, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/typesize?module=" + esc(module) + "&type=" + esc(type), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupFieldOffset: {
            description: "Looks up the type, offset, and size of a field in a given type.",
            arguments: [
                {name:"module", type:"string", description:"The module of the type."},
                {name:"type", type:"string", description:"The type."},
                {name:"field", type:"string", description:"The name of the field to lookup."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupFieldOffset: function(module, type, field, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/fieldoffset?module=" + esc(module) + "&type=" + esc(type) + "&field=" + esc(field), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupFields: {
            description: "Gets all the fields available within a given type.",
            arguments: [
                {name:"module", type:"string", description:"The module of the type."},
                {name:"type", type:"string", description:"The type."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupFields: function(module, type, includeBaseTypes, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/typefields?module=" + esc(module) + "&type=" + esc(type) + "&includeBaseTypes=" + esc(includeBaseTypes), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupBaseTypes: {
            description: "Looks up the names and offsets of the base types of a type.",
            arguments: [
                {name:"module", type:"string", description:"The module of the type."},
                {name:"type", type:"string", description:"The type."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupBaseTypes: function(module, type, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/basetypes?module=" + esc(module) + "&type=" + esc(type), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_ReadNumber: {
            description: "Reads a number value from memory.",
            arguments: [
                {name:"pointer", type:"integer", description:"The pointer to the number."},
                {name:"size", type:"integer", description:"The size of the number."},
                {name:"isUnsigned", type:"bool", description:"A value that indicates if the number is unsigned."},
                {name:"isFloat", type:"bool", description:"A value that indicates if the number is a floating point number."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        ReadNumber: function(pointer, size, isUnsigned, isFloat, callback) {
            var sizeName = getSizeName(size, isUnsigned, isFloat);
            if (sizeName == null) {
                callback({ "error": "Invalid number size." });
                return;
            }

            if (isFloat) {
                var originalCallback = callback;
                callback = function(result) {
                    if (typeof(result.value) != typeof(undefined)) {
                        result = {value: readJsonFloat(result.value)};
                    }
                    originalCallback(result);
                }
            } else {
                // Make it a bigInt.
                var originalCallback = callback;
                callback = function(result) {
                    if (typeof(result.value) != typeof(undefined)) {
                        result = {value: bigInt(result.value) };
                    }
                    originalCallback(result);
                };
            }

            JsDbgTransport.JsonRequest("/jsdbg-server/memory?type=" + esc(sizeName) + "&pointer=" + esc(pointer), callback, JsDbgTransport.CacheType.TransientCache);
        },

        _help_WriteNumber: {
            description: "Writes a number value in memory.",
            arguments: [
                {name:"pointer", type:"integer", description:"The pointer to the number."},
                {name:"size", type:"integer", description:"The size of the number."},
                {name:"isUnsigned", type:"bool", description:"A value that indicates if the number is unsigned."},
                {name:"isFloat", type:"bool", description:"A value that indicates if the number is a floating point number."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        WriteNumber: function(pointer, size, isUnsigned, isFloat, value, callback) {
            var sizeName = getSizeName(size, isUnsigned, isFloat);
            if (sizeName == null) {
                callback({ "error": "Invalid number size." });
                return;
            }

            if (!isFloat) {
                value = bigInt(value);
            }

            var originalCallback = callback;
            callback = function(result) {
                if (!result.error) {
                    JsDbgTransport.InvalidateCache();
                    fireListeners(memoryWriteListeners);
                }
                originalCallback(result);
            }

            JsDbgTransport.JsonRequest("/jsdbg-server/writememory?type=" + esc(sizeName) + "&pointer=" + esc(pointer) + "&value=" + esc(value), callback, JsDbgTransport.CacheType.Uncached);
        },

        _help_ReadArray: {
            description: "Reads an array of number values from memory.",
            arguments: [
                {name:"pointer", type:"integer", description:"The pointer to the first number."},
                {name:"size", type:"integer", description:"The size of each number."},
                {name:"isUnsigned", type:"bool", description:"A value that indicates if the numbers are unsigned."},
                {name:"isFloat", type:"bool", description:"A value that indicates if the numbers are floating point numbers."},
                {name:"count", type:"integer", description:"The count of numbers to read."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        ReadArray: function(pointer, itemSize, isUnsigned, isFloat, count, callback) {
            var sizeName = getSizeName(itemSize, isUnsigned, isFloat);
            if (sizeName == null) {
                callback({ "error": "Invalid number size." });
                return;
            }

            if (isFloat) {
                var originalCallback = callback;
                callback = function(result) {
                    if (typeof(result.array) != typeof(undefined)) {
                        result = {array: result.array.map(readJsonFloat) };
                    }
                    originalCallback(result);
                }
            } else {
                // Make the numbers bigInts.
                var originalCallback = callback;
                callback = function(result) {
                    if (typeof(result.array) != typeof(undefined)) {
                        result = {array: result.array.map(function (n) { return bigInt(n); }) };
                    }
                    originalCallback(result);
                };
            }

            JsDbgTransport.JsonRequest("/jsdbg-server/array?type=" + esc(sizeName) + "&pointer=" + esc(pointer) + "&length=" + count, callback, JsDbgTransport.CacheType.TransientCache);
        },

        _help_LookupSymbolName: {
            description: "Identifies a symbol associated with a given pointer (e.g. vtable pointer).",
            arguments: [
                {name:"pointer", type:"integer", description:"The pointer to the first number."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupSymbolName: function(pointer, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/symbolname?pointer=" + esc(pointer), callback, JsDbgTransport.CacheType.TransientCache);
        },

        _help_IsTypeEnum: {
            description: "Indicates if a given type is an enum.",
            arguments: [
                {name: "module", type:"string", description: "The module of the type."},
                {name: "type", type:"string", description: "The type."},
                {name: "callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        IsTypeEnum: function(module, type, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/isenum?module=" + esc(module) + "&type=" + esc(type), callback, JsDbgTransport.CacheType.Cached);
        },

        
        _help_LookupConstantName: {
            description: "Looks up the name of a given constant (i.e. an enum value).",
            arguments: [
                {name:"module", type:"string", description:"The module of the type."},
                {name:"type", type:"string", description:"The type."},
                {name:"constant", type:"integer", description:"The constant."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupConstantName: function(module, type, constant, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/constantname?module=" + esc(module) + "&type=" + esc(type) + "&constant=" + esc(constant), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupConstantValue: {
            description: "Looks up the value of a given constant (i.e. an enum value).",
            arguments: [
                {name:"module", type:"string", description:"The module of the enum type."},
                {name:"type", type:"string", description:"The enum type."},
                {name:"constantName", type:"string", description:"The constant name (i.e. enum name)."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupConstantValue: function(module, type, constantName, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/constantvalue?module=" + esc(module) + "&type=" + esc(type) + "&name=" + esc(constantName), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupGlobalSymbol: {
            description: "Evaluates a global symbol and returns the type and address of the value.",
            arguments: [
                {name:"module", type:"string", description:"The module containing the symbol."},
                {name:"symbol", type:"string", description:"The symbol to evaluate."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupGlobalSymbol: function(module, symbol, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/global?module=" + esc(module) + "&symbol=" + esc(symbol), callback, JsDbgTransport.CacheType.Cached);
        },

        _help_LookupLocalSymbols: {
            description: "Evaluates a local symbolic expression and returns the type of and pointer to the each value on the stack.",
            arguments: [
                {name:"module", type:"string", description:"The module containing the method."},
                {name:"method", type:"string", description:"The method whose local symbol should be retrieved."},
                {name:"symbol", type:"string", description:"The symbolic expression to evaluate."},
                {name:"maxCount", type:"int", description:"The maximum number of stack frames to collect from."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        LookupLocalSymbols: function(module, method, symbol, maxCount, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/localsymbols?module=" + esc(module) + "&method=" + esc(method) + "&symbol=" + esc(symbol) + "&maxCount=" + esc(maxCount), callback, JsDbgTransport.CacheType.TransientCache);
        },

        _help_GetPersistentData: {
            description: "Gets the persistent data associated with the current user or a specified user.",
            arguments: [
                {name:"user", type:"string", description:"(optional) The user whose data should be retrieved."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        GetPersistentData: function(user, callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/persistentstorage" + (user ? "?user=" + esc(user) : ""), callback, JsDbgTransport.CacheType.Uncached, "GET");
        },

        _help_SetPersistentData: {
            description: "Saves the persistent data associated with the current user.",
            arguments: [
                {name:"data", type:"object", description:"The object to save."},
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        SetPersistentData: function(data, callback) {
            var value = JSON.stringify(data);
            JsDbgTransport.JsonRequest("/jsdbg-server/persistentstorage", callback, JsDbgTransport.CacheType.Uncached, "PUT", value);
        },

        _help_GetPersistentDataUsers: {
            description: "Gets a collection of users with persistent data stored.",
            arguments: [
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        GetPersistentDataUsers: function(callback) {
            JsDbgTransport.JsonRequest("/jsdbg-server/persistentstorageusers", callback, JsDbgTransport.CacheType.Uncached);
        },

        _help_SendFeedback: {
            description: "Sends feedback for JsDbg.",
            arguments: [
                {name:"callback", type:"function(object)", description:"A callback that is called when the operation succeeds or fails."}
            ]
        },
        SendFeedback: function (message, callback) {
            // Include some diagnostics data as well.
            var feedbackObject = {
                userAgent: window.navigator.userAgent,
                extension: Loader.GetCurrentExtension,
                message: message
            };

            JsDbgTransport.JsonRequest("/jsdbg-server/feedback", callback, JsDbgTransport.CacheType.Uncached, "PUT", JSON.stringify(feedbackObject, null, '  '));
        },

        _help_RegisterOnBreakListener: {
            description: "Registers a callback to be fired when the debugger breaks in.",
            arguments: [
                {name: "callback", type:"function()", description:"A callback that is called when the debugger breaks in to the target."}
            ]
        },
        RegisterOnBreakListener: function(callback) {
            debuggerBrokeListeners.push(callback);
        },

        _help_RegisterOnMemoryWriteListener: {
            description: "Registers a callback to be fired whenever JsDbg writes to memory.",
            arguments: [
                {name: "callback", type:"function()", description:"A callback that is called when JsDbg writes to memory."}
            ]
        },
        RegisterOnMemoryWriteListener: function (callback) {
            memoryWriteListeners.push(callback);
        }
    }

    Help.Register(JsDbg);
})