﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Mono.Cecil;

using IL2C.Translators;

namespace IL2C
{
    public enum DebugInformationOptions
    {
        None,
        CommentOnly,
        Full
    }

    public static class AssemblyWriter
    {
        private static void InternalConvertType(
            TextWriter tw,
            IExtractContext extractContext,
            TypeDefinition declaredType,
            string indent)
        {
            if (declaredType.IsPrimitive
                || !(declaredType.IsValueType || declaredType.IsClass))
            {
                return;
            }

            var structName = extractContext.GetCLanguageTypeName(
                declaredType,
                TypeNameFlags.Dereferenced)
                .ManglingSymbolName();

            var rawTypeName = declaredType
                .GetFullMemberName()
                .ManglingSymbolName();
            var classOrStruct = declaredType.IsValueType ? "Struct" : "Class";

            tw.WriteLine("////////////////////////////////////////////////////////////");
            tw.WriteLine(
                "// {0}: {1}",
                classOrStruct,
                declaredType.GetFullMemberName());
            tw.WriteLine();

            var virtualMethods = declaredType
                .EnumerateOrderedOverridedMethods()
                .ToArray();
            if (virtualMethods.Length >= 1)
            {
                tw.WriteLine(
                    "// {0} vtable layout",
                    classOrStruct);
                tw.WriteLine("typedef const struct");
                tw.WriteLine("{");
                tw.WriteLine(
                    "{0}/* internalcall */ void* (*IL2C_RuntimeCast)({1}* this__, IL2C_RUNTIME_TYPE_DECL* type);",
                    indent,
                    rawTypeName);
                virtualMethods.ForEach(method =>
                {
                    var functionPrototype = Utilities.GetFunctionTypeString(
                        method.GetOverloadedMethodName().ManglingSymbolName(),
                        method.ReturnType,
                        method.GetSafeParameters(declaredType),
                        extractContext);
                    tw.WriteLine(
                        "{0}{1};",
                        indent,
                        functionPrototype);
                });

                tw.WriteLine(
                    "}} __{0}_VTABLE_DECL__;",
                    rawTypeName);
                tw.WriteLine();
            }

            var instanceFields = declaredType
                .Traverse(type => type.BaseType?.Resolve())
                .Reverse()
                .SelectMany(type => type.Fields.Where(field => !field.IsStatic))
                .ToArray();
            if ((declaredType.IsValueType == false) ||
                (instanceFields.Length >= 1))
            {
                tw.WriteLine(
                    "// {0} layout",
                    classOrStruct);
                tw.WriteLine(
                    "struct {0}",
                    structName);
                tw.WriteLine("{");

                if (declaredType.IsValueType == false)
                {
                    tw.WriteLine(
                        "{0}// Instance's vptr",
                        indent);
                    tw.WriteLine(
                        "{0}__{1}_VTABLE_DECL__* vptr0__;",
                        indent,
                        rawTypeName);
                    tw.WriteLine();
                }
                instanceFields.ForEach(field =>
                    tw.WriteLine(
                        "{0}{1} {2};",
                        indent,
                        extractContext.GetCLanguageTypeName(field.FieldType),
                        field.Name));

                tw.WriteLine("};");
                tw.WriteLine();
            }

            var makrHandlerPrototype = string.Format(
                "extern IL2C_RUNTIME_TYPE_DECL __{0}_RUNTIME_TYPE__;",
                rawTypeName);
            tw.WriteLine(
                "// {0} runtime type information",
                classOrStruct);
            tw.WriteLine(makrHandlerPrototype);
        }

        private static string GetFunctionNameByFunctionType(PreparedFunction preparedFunction)
        {
            return (preparedFunction.FunctionType == FunctionTypes.Virtual)
                ? ("__" + preparedFunction.MethodName + "__")
                : preparedFunction.MethodName;
        }

        private static void InternalConvertToPrototypes(
            TextWriter tw,
            TypeDefinition[] types,
            IExtractContext extractContext,
            PreparedFunctions preparedFunctions,
            string indent)
        {
            tw.WriteLine();
            tw.WriteLine("#ifdef __cplusplus");
            tw.WriteLine("extern \"C\" {");
            tw.WriteLine("#endif");

            tw.WriteLine();
            tw.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            tw.WriteLine("// Types:");
            tw.WriteLine();

            // Output prototypes.
            types.ForEach(type =>
            {
                var typeName = extractContext.GetCLanguageTypeName(type, TypeNameFlags.Dereferenced)
                    .ManglingSymbolName();
                tw.WriteLine(
                    "typedef struct {0} {0};",
                    typeName);
            });

            // Output value type and object reference type.
            types.ForEach(type =>
            {
                tw.WriteLine();
                InternalConvertType(
                    tw,
                    extractContext,
                    type,
                    indent);
            });

            tw.WriteLine();
            tw.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            tw.WriteLine("// Public static fields:");

            types.ForEach(type =>
            {
                tw.WriteLine();
                type.Fields
                    .Where(field => field.IsPublic && field.IsStatic)
                    .ForEach(field =>
                    {
                        tw.WriteLine(
                            "extern {0};",
                            Utilities.GetStaticFieldPrototypeString(field, false, extractContext));
                    });
            });

            tw.WriteLine();
            tw.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            tw.WriteLine("// Methods:");

            types.ForEach(type =>
            {
                var rawTypeName = type
                    .GetFullMemberName()
                    .ManglingSymbolName();
                var typeName = extractContext
                    .GetCLanguageTypeName(type, TypeNameFlags.Dereferenced)
                    .ManglingSymbolName();

                tw.WriteLine();
                tw.WriteLine(
                    "// {0}",
                    type.FullName);
                tw.WriteLine(
                    "extern /* internalcall */ void __{0}_IL2C_MarkHandler__({1}* this__);",
                    rawTypeName,
                    typeName);
                tw.WriteLine(
                    "extern /* internalcall */ void* __{0}_IL2C_RuntimeCast__({1}* this__, IL2C_RUNTIME_TYPE_DECL* type);",
                    rawTypeName,
                    typeName);

                type.Methods
                    .Where(method => !method.IsConstructor || !method.IsStatic)
                    .ForEach(method =>
                    {
                        var preparedFunction = preparedFunctions.Functions[method];

                        var functionPrototype = Utilities.GetFunctionPrototypeString(
                            GetFunctionNameByFunctionType(preparedFunction),
                            preparedFunction.ReturnType,
                            preparedFunction.Parameters,
                            extractContext);

                        tw.WriteLine(
                            "extern {0}{1};",
                            method.IsVirtual ? "/* virtual */ " : string.Empty,
                            functionPrototype);
                    });

                var virtualMethods = type
                    .EnumerateOrderedOverridedMethods()
                    .ToArray();
                if (virtualMethods.Length >= 1)
                {
                    virtualMethods.ForEach(method =>
                    {
                        var fullMethodName = type
                            .GetFullMemberName(method, MethodNameTypes.Index)
                            .ManglingSymbolName();
                        var functionParametersDeclaration = string.Join(
                            ", ",
                            method.GetSafeParameters()
                                .Select((parameter, index) => (index == 0)
                                    ? string.Format(
                                        "/* {0} */ {1}",
                                        extractContext.GetCLanguageTypeName(type),
                                        parameter.Name)
                                    : string.Format(
                                        "/* {0} */ {1}",
                                        extractContext.GetCLanguageTypeName(parameter.ParameterType),
                                        parameter.Name)));
                        tw.WriteLine(
                            "#define {0}({1}) \\",
                            fullMethodName,
                            functionParametersDeclaration);

                        var methodName = method
                            .GetOverloadedMethodName()
                            .ManglingSymbolName();
                        var functionParameters = string.Join(
                            ", ",
                            method.GetSafeParameters()
                                .Select(parameter => parameter.Name));

                        tw.WriteLine(
                            "{0}((this__)->vptr0__->{1}({2}))",
                            indent,
                            methodName,
                            functionParameters);
                    });
                }
            });

            tw.WriteLine();
            tw.WriteLine("#ifdef __cplusplus");
            tw.WriteLine("}");
            tw.WriteLine("#endif");
        }

        private static void InternalConvertFromFunction(
            TextWriter tw,
            IExtractContext extractContext,
            PreparedFunction preparedFunction,
            string indent,
            DebugInformationOptions debugInformationOption)
        {
            var locals = preparedFunction.LocalVariables;

            var functionPrototype = Utilities.GetFunctionPrototypeString(
                GetFunctionNameByFunctionType(preparedFunction),
                preparedFunction.ReturnType,
                preparedFunction.Parameters,
                extractContext);

            tw.WriteLine();
            tw.WriteLine("///////////////////////////////////////");
            tw.WriteLine(
                "// {0}{1}",
                (preparedFunction.FunctionType == FunctionTypes.Virtual)
                    ? "Virtual: "
                    : string.Empty,
                preparedFunction.RawMethodName);
            tw.WriteLine();

            tw.WriteLine(functionPrototype);
            tw.WriteLine("{");

            tw.WriteLine("{0}//-------------------", indent);
            tw.WriteLine("{0}// Local variables:", indent);
            tw.WriteLine();

            // Important NULL assigner (p = NULL):
            //   Because these variables are pointer (of object reference).
            //   So GC will traverse these variables just setup the stack frame.

            preparedFunction.LocalVariables.ForEach(local =>
            {
                tw.WriteLine(
                    "{0}{1} {2}{3};",
                    indent,
                    extractContext.GetCLanguageTypeName(local.TargetType),
                    local.SymbolName,
                    local.TargetType.IsValueType ? string.Empty : " = NULL");
            });

            tw.WriteLine();
            tw.WriteLine("{0}//-------------------", indent);
            tw.WriteLine("{0}// Evaluation stacks:", indent);
            tw.WriteLine();

            preparedFunction.Stacks.ForEach(si =>
            {
                tw.WriteLine(
                    "{0}{1} {2}{3};",
                    indent,
                    extractContext.GetCLanguageTypeName(si.TargetType),
                    si.SymbolName,
                    si.TargetType.IsValueType ? string.Empty : " = NULL");
            });

            var frameEntries = locals
                .Concat(preparedFunction.Stacks)
                .Where(local => !local.TargetType.IsValueType && !local.TargetType.IsPointer)
                .ToArray();

            if (frameEntries.Length >= 1)
            {
                tw.WriteLine();
                tw.WriteLine("{0}//-------------------", indent);
                tw.WriteLine("{0}// Setup stack frame:", indent);
                tw.WriteLine();

                tw.WriteLine("{0}struct /* IL2C_EXECUTION_FRAME */", indent);
                tw.WriteLine("{0}{{", indent);
                tw.WriteLine("{0}{0}IL2C_EXECUTION_FRAME* pNext;", indent);
                tw.WriteLine("{0}{0}uint8_t targetCount;", indent);

                frameEntries.ForEach(frameEntry =>
                {
                    tw.WriteLine(
                        "{0}{0}{1}* p{2};",
                        indent,
                        extractContext.GetCLanguageTypeName(frameEntry.TargetType),
                        frameEntry.SymbolName);
                });

                tw.WriteLine("{0}}} __executionFrame__;", indent);
                tw.WriteLine();
                tw.WriteLine("{0}__executionFrame__.targetCount = {1};", indent, frameEntries.Length);

                frameEntries.ForEach(frameEntry =>
                {
                    tw.WriteLine(
                        "{0}__executionFrame__.p{1} = &{1};",
                        indent,
                        frameEntry.SymbolName);
                });

                tw.WriteLine("{0}il2c_link_execution_frame(&__executionFrame__);", indent);
            }

            tw.WriteLine();
            tw.WriteLine("{0}//-------------------", indent);
            tw.WriteLine("{0}// IL body:", indent);
            tw.WriteLine();

            var canWriteSequencePoint = true;
            preparedFunction.PreparedILBodies.ForEach(ilBody =>
            {
                // Write label if available and used.
                if (preparedFunction.TryGetLabelName(
                    ilBody.Label, out var labelName))
                {
                    tw.WriteLine("{0}:", labelName);
                }

                // Write the line preprocessor directive if available.
                if (canWriteSequencePoint && ilBody.SequencePoints.Any())
                {
                    var sp = ilBody.SequencePoints.First();
                    switch (debugInformationOption)
                    {
                        case DebugInformationOptions.Full:
                            tw.WriteLine(
                                "#line {0} \"{1}\"",
                                sp.StartLine,
                                sp.Document.Url.Replace("\\", "\\\\"));
                            break;
                        case DebugInformationOptions.CommentOnly:
                            tw.WriteLine(
                                "/* {0}({1}): */",
                                sp.Document.Url.Replace("\\", "\\\\"),
                                sp.StartLine);
                            break;
                    }

                    canWriteSequencePoint = false;
                }

                if (debugInformationOption != DebugInformationOptions.None)
                {
                    // Write debugging information.
                    tw.WriteLine(
                        "{0}/* {1} */",
                        indent,
                        ilBody);
                }

                // Generate source code fragments and write.
                var sourceCodes = ilBody.Generator(extractContext);
                sourceCodes.ForEach(sourceCode =>
                {
                    // Dirty hack:
                    //   Write unlink execution frame code if cause exiting method.
                    if (sourceCode.StartsWith("return")
                        && (frameEntries.Length >= 1))
                    {
                        tw.WriteLine(
                            "{0}il2c_unlink_execution_frame(&__executionFrame__);",
                            indent);
                    }

                    tw.WriteLine(
                        "{0}{1};",
                        indent,
                        sourceCode);

                    canWriteSequencePoint = true;
                });
            });

            tw.WriteLine("}");
        }

        private static void InternalConvertFromAbstractFunction(
            TextWriter tw,
            IExtractContext extractContext,
            PreparedFunction preparedFunction,
            string indent)
        {
            var functionPrototype = Utilities.GetFunctionPrototypeString(
                GetFunctionNameByFunctionType(preparedFunction),
                preparedFunction.ReturnType,
                preparedFunction.Parameters,
                extractContext);

            tw.WriteLine();
            tw.WriteLine("///////////////////////////////////////");
            tw.WriteLine("// Abstract: {0}", preparedFunction.RawMethodName);
            tw.WriteLine();

            tw.WriteLine(functionPrototype);
            tw.WriteLine("{");

            tw.WriteLine(
                "{0}// WARNING: Pure virtual function called.",
                indent);
            tw.WriteLine(
                "{0}//TODO: throw : assert(0);",
                indent);

            if (preparedFunction.ReturnType.IsVoidType() == false)
            {
                tw.WriteLine(
                    "{0}return ({1}){2};",
                    indent,
                    extractContext.GetCLanguageTypeName(preparedFunction.ReturnType),
                    preparedFunction.ReturnType.IsNumericPrimitive() ? "0" : "NULL");
            }

            tw.WriteLine("}");
        }

        private static void InternalConvertFromPInvokeFunction(
            TextWriter tw,
            IExtractContext extractContext,
            PreparedFunction preparedFunction,
            PInvokeInfo pinvokeInfo,
            string indent)
        {
            var functionPrototype = Utilities.GetFunctionPrototypeString(
                GetFunctionNameByFunctionType(preparedFunction),
                preparedFunction.ReturnType,
                preparedFunction.Parameters,
                extractContext);

            tw.WriteLine();
            tw.WriteLine("///////////////////////////////////////");
            tw.WriteLine("// P/Invoke: {0}", preparedFunction.RawMethodName);
            tw.WriteLine();

            tw.WriteLine(functionPrototype);
            tw.WriteLine("{");

            var arguments = string.Join(
                ", ",
                preparedFunction.Parameters.Select(parameter => parameter.GetMarshaledInExpression()));

            if (preparedFunction.ReturnType.IsVoidType())
            {
                tw.WriteLine("{0}{1}({2});", indent, pinvokeInfo.EntryPoint, arguments);
            }
            else
            {
                tw.WriteLine("{0}return {1}({2});", indent, pinvokeInfo.EntryPoint, arguments);
            }

            tw.WriteLine("}");
        }

        private static void InternalConvertTypeHelper(
            TextWriter tw,
            IExtractContext extractContext,
            TypeDefinition declaredType,
            string indent)
        {
            tw.WriteLine();
            tw.WriteLine("//////////////////////");
            tw.WriteLine("// Runtime helpers:");

            var rawTypeName = declaredType
                .GetFullMemberName()
                .ManglingSymbolName();
            var typeName = extractContext.GetCLanguageTypeName(
                declaredType,
                TypeNameFlags.Dereferenced);
            var rawBaseTypeName = declaredType.BaseType
                .GetFullMemberName()
                .ManglingSymbolName();

            // Write RuntimeCast function:
            tw.WriteLine();
            tw.WriteLine("// Runtime cast");
            tw.WriteLine(
                "void* __{0}_IL2C_RuntimeCast__({1}* this__, IL2C_RUNTIME_TYPE_DECL* type)",
                rawTypeName,
                typeName);
            tw.WriteLine("{");
            // TODO: interfaces
            tw.WriteLine(
                "{0}if (type == il2c_typeof({1})) return this__;",
                indent,
                rawBaseTypeName);
            tw.WriteLine(
                "{0}return __{1}_IL2C_RuntimeCast__(({1}*)this__, type);",
                indent,
                rawBaseTypeName);
            tw.WriteLine("}");

            // Write mark handler:
            var makrHandlerPrototype = string.Format(
                "void __{0}_IL2C_MarkHandler__({1}* this__)",
                rawTypeName,
                typeName);

            tw.WriteLine();
            tw.WriteLine("// GC's mark handler");
            tw.WriteLine(makrHandlerPrototype);
            tw.WriteLine("{");

            declaredType.Fields
                .Where(field => !field.IsStatic && !field.FieldType.IsValueType)
                .ForEach(field =>
                {
                    tw.WriteLine(
                        "{0}il2c_try_mark_from_handler(this__->{1});",
                        indent,
                        field.Name);
                });

            // Invoke base class mark handler except System.Object and System.ValueType.
            var baseType = declaredType.BaseType;
            if (baseType != null)
            {
                if ((baseType.IsObjectType() || baseType.IsValueTypeType()) == false)
                {
                    tw.WriteLine(
                        "{0}__{1}_IL2C_MarkHandler__(({1}*)this__);",
                        indent,
                        rawBaseTypeName);
                }
                else
                {
                    tw.WriteLine(
                        "{0}/* Suppressed invoke base mark handler */",
                        indent);
                }
            }

            tw.WriteLine("}");

            // Write virtual methods
            tw.WriteLine();
            tw.WriteLine(
                "static __{0}_VTABLE_DECL__ __{0}_VTABLE__ = {{",
                rawTypeName);
            tw.WriteLine(
                "{0}/* internalcall */ __{1}_IL2C_RuntimeCast__,",
                indent,
                rawTypeName);
            declaredType
                .EnumerateOrderedOverridedMethods()
                .ForEach(method =>
                {
                    tw.WriteLine(
                        "{0}(void*)__{1}__,",
                        indent,
                        method.GetFullMemberName(MethodNameTypes.Index).ManglingSymbolName());
                });
            tw.WriteLine("};");

            // Write runtime type information
            tw.WriteLine();
            tw.WriteLine(
                "IL2C_RUNTIME_TYPE_DECL __{0}_RUNTIME_TYPE__ = {{",
                rawTypeName);
            tw.WriteLine(
                "{0}\"{1}\",",
                indent,
                declaredType.GetFullMemberName());
            tw.WriteLine(
                "{0}sizeof({1}),",
                indent,
                typeName);
            tw.WriteLine(
                "{0}/* internalcall */ (void*)__{1}_IL2C_MarkHandler__,",
                indent,
                rawTypeName);

            tw.WriteLine("};");
        }

        internal static void InternalConvertFromMethod(
            TextWriter tw,
            IExtractContext extractContext,
            PreparedFunctions preparedFunctions,
            MethodDefinition method,
            string indent,
            DebugInformationOptions debugInformationOption = DebugInformationOptions.None)
        {
            var methodName = method.GetFullMemberName();
            var preparedFunction = preparedFunctions.Functions[method];

            // Write method body
            switch (preparedFunction.FunctionType)
            {
                case FunctionTypes.Standard:
                    Debug.Assert(preparedFunction.PreparedILBodies != null);

                    InternalConvertFromFunction(
                        tw,
                        extractContext,
                        preparedFunction,
                        indent,
                        debugInformationOption);
                    break;

                case FunctionTypes.Virtual:
                    if (preparedFunction.PreparedILBodies != null)
                    {
                        InternalConvertFromFunction(
                            tw,
                            extractContext,
                            preparedFunction,
                            indent,
                            debugInformationOption);
                    }
                    else
                    {
                        InternalConvertFromAbstractFunction(
                            tw,
                            extractContext,
                            preparedFunction,
                            indent);
                    }
                    break;

                case FunctionTypes.PInvoke:
                    var pinvokeInfo = method.PInvokeInfo;
                    if (pinvokeInfo == null)
                    {
                        throw new InvalidProgramSequenceException(
                            "Missing DllImport attribute at P/Invoke entry: Method={0}",
                            methodName);
                    }

                    InternalConvertFromPInvokeFunction(
                        tw,
                        extractContext,
                        preparedFunction,
                        pinvokeInfo,
                        indent);
                    break;
            }
        }

        public static void WriteHeader(
            TextWriter twHeader,
            TranslateContext translateContext,
            PreparedFunctions preparedFunctions,
            string indent)
        {
            IExtractContext extractContext = translateContext;

            var assemblyName = extractContext.Assembly.Name.Name;
            var safeSymbolName = assemblyName.Replace('.', '_').Replace('-', '_');

            twHeader.WriteLine("#ifndef __MODULE_{0}__", safeSymbolName);
            twHeader.WriteLine("#define __MODULE_{0}__", safeSymbolName);
            twHeader.WriteLine();

            extractContext.EnumerateRequiredIncludeFileNames()
                .ForEach(fileName => twHeader.WriteLine("#include <{0}>", fileName));

            var types = extractContext.Assembly.Modules
                .SelectMany(module => module.Types)
                // All types exclude privates
                .Where(type => (type.IsValueType || type.IsClass)
                    && (type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly))
                .ToArray();

            InternalConvertToPrototypes(
                twHeader,
                types,
                extractContext,
                preparedFunctions,
                indent);

            twHeader.WriteLine();
            twHeader.WriteLine("#endif");
        }

        public static void WriteSourceCode(
            TextWriter twSource,
            TranslateContext translateContext,
            PreparedFunctions preparedFunctions,
            string indent,
            DebugInformationOptions debugInformationOption = DebugInformationOptions.Full)
        {
            IExtractContext extractContext = translateContext;

            extractContext.EnumerateRequiredPrivateIncludeFileNames()
                .ForEach(fileName => twSource.WriteLine("#include <{0}>", fileName));

            var assemblyName = extractContext.Assembly.Name.Name;
            twSource.WriteLine("#include \"{0}.h\"", assemblyName);

            twSource.WriteLine();
            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            twSource.WriteLine("// Const strings:");
            twSource.WriteLine();

            extractContext.ExtractConstStrings()
                .ForEach(kv =>
                {
                    var escaped = Utilities.GetEscapedCString(kv.Value);
                    twSource.WriteLine(
                        "IL2C_CONST_STRING({0}, L\"{1}\");",
                        kv.Key,
                        escaped);
                });

            var allTypes = extractContext.Assembly.Modules
                .SelectMany(module => module.Types)
                .Where(type => type.IsValidDefinition())
                .ToArray();

            // All types exclude publics and internals (for file scope prototypes)
            var types = allTypes
                .Where(type => !(type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly))
                .ToArray();

            InternalConvertToPrototypes(
                twSource,
                types,
                extractContext,
                preparedFunctions,
                indent);

            twSource.WriteLine();
            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            twSource.WriteLine("// Static fields:");

            allTypes.ForEach(type =>
            {
                twSource.WriteLine();

                // All static fields
                type.Fields
                    .Where(field => field.IsStatic)
                    .ForEach(field => twSource.WriteLine(
                        "{0};",
                        Utilities.GetStaticFieldPrototypeString(field, true, extractContext)));
            });

            twSource.WriteLine();
            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
            twSource.WriteLine("// Methods:");

            allTypes.ForEach(type =>
            {
                twSource.WriteLine();
                twSource.WriteLine("////////////////////////////////////////////////////////////");
                twSource.WriteLine("// Type: {0}", type.GetFullMemberName());

                // All methods and constructor exclude type initializer
                type.Methods
                    .Where(method => !method.IsConstructor || !method.IsStatic)
                    .ForEach(method =>
                    {
                        InternalConvertFromMethod(
                            twSource,
                            extractContext,
                            preparedFunctions,
                            method,
                            indent,
                            debugInformationOption);
                    });

                InternalConvertTypeHelper(
                    twSource,
                    extractContext,
                    type,
                    indent);
            });
        }
    }
}
