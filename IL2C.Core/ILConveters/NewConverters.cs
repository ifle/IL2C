﻿using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using IL2C.Translators;

namespace IL2C.ILConveters
{
    internal sealed class InitobjConverter : InlineTypeConverter
    {
        public override OpCode OpCode => OpCodes.Initobj;

        public override Func<IExtractContext, string[]> Apply(
            TypeReference type, DecodeContext decodeContext)
        {
            var si = decodeContext.PopStack();
            if (si.TargetType.IsByReference == false)
            {
                throw new InvalidProgramSequenceException(
                    "Invalid type at stack: Offset={0}, TokenType={1}, StackType={2}",
                    decodeContext.Current.Offset,
                    type.FullName,
                    si.TargetType.FullName);
            }

            decodeContext.PrepareContext.RegisterIncludeFile("string.h");

            return extractContext =>
            {
                var typeName = extractContext.GetCLanguageTypeName(type);

                return new[] { string.Format(
                    "memset({0}, 0x00, sizeof({1}))",
                    si.SymbolName,
                    typeName) };
            };
        }
    }

    internal sealed class NewobjConverter : InlineMethodConverter
    {
        public override OpCode OpCode => OpCodes.Newobj;

        public override Func<IExtractContext, string[]> Apply(
            MethodReference method, DecodeContext decodeContext)
        {
            var md = method.Resolve();
            if (!md.IsConstructor)
            {
                throw new InvalidProgramSequenceException(
                    "Invalid new object constructor: Offset={0}, Method={1}",
                    decodeContext.Current.Offset,
                    md.GetFullMemberName());
            }

            var pairParameters = md.Parameters
                .Reverse()
                .Select(parameter => new Utilities.RightExpressionGivenParameter(
                    parameter.ParameterType, decodeContext.PopStack()))
                .Reverse()
                .ToList();
            var overloadIndex = method.GetMethodOverloadIndex();

            var type = md.DeclaringType;
            var thisSymbolName = decodeContext.PushStack(type);

            // Insert this reference.
            pairParameters.Insert(0,
                new Utilities.RightExpressionGivenParameter(
                    type, new SymbolInformation(thisSymbolName, type)));

            var offset = decodeContext.Current.Offset;

            return extractContext =>
            {
                var parameterString = Utilities.GetGivenParameterDeclaration(
                    pairParameters.ToArray(), extractContext, offset);

                // newobj opcode can handle value type with parameter applied constructor.
                if (type.IsValueType)
                {
                    var typeName = extractContext.GetCLanguageTypeName(
                        type);
                    // If constructor's arguments greater than or equal 2 (this and others)
                    if (pairParameters.Count >= 2)
                    {
                        return new[] {
                            string.Format(
                                "memset(&{0}, 0x00, sizeof({1}))",
                                thisSymbolName,
                                typeName),
                            (overloadIndex >= 1)
                                ? string.Format(
                                    "{0}__ctor_{1}(&{2})",
                                    typeName,
                                    overloadIndex,
                                    parameterString)
                                : string.Format(
                                    "{0}__ctor(&{1})",
                                    typeName,
                                    parameterString)
                        };
                    }
                    else
                    {
                        // ValueType's default constructor not declared.
                        return new[] { string.Format(
                            "memset(&{0}, 0x00, sizeof({1}))",
                            thisSymbolName,
                            typeName) };
                    }
                }
                // Object reference types.
                else
                {
                    var dereferencedTypeName = extractContext.GetCLanguageTypeName(
                        type, TypeNameFlags.Dereferenced);

                    var get = new[]
                    {
                        string.Format(
                            "{0} = il2c_get_uninitialized_object(il2c_typeof({1}))",
                            thisSymbolName,
                            dereferencedTypeName)
                    };

                    // Setup vptr from vtables.
                    var vptr = new[]
                    {
                        // Instance's vptr
                        string.Format(
                            "{0}->vptr0__ = &__{1}_VTABLE__",
                            thisSymbolName,
                            dereferencedTypeName)
                    }.Concat(type.Interfaces.Select(interfaceImpl =>
                    {
                        // Interface's vptr:
                        //   These are unique tables by pair of instance type and interface type.
                        //   Because vtable has function pointers from unique adjustor thunk by instance type layout offset.
                        var tn = extractContext.GetCLanguageTypeName(
                            interfaceImpl.InterfaceType, TypeNameFlags.Dereferenced);
                        return string.Format(
                            "{0}->vptr_{1}__ = &__{2}_{1}_VTABLE__",
                            thisSymbolName,
                            tn,
                            dereferencedTypeName);
                    }));

                    var ctor = new[]
                    {
                        (overloadIndex >= 1)
                            ? string.Format(
                                "{0}__ctor_{1}({2})",
                                dereferencedTypeName,
                                overloadIndex,
                                parameterString)
                            : string.Format(
                                "{0}__ctor({1})",
                                dereferencedTypeName,
                                parameterString)
                    };

                    return get
                        .Concat(vptr)
                        .Concat(ctor)
                        .ToArray();
                }
            };
        }
    }
}
