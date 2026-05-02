using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor
    {
        private static IReadOnlyList<FieldDefinition> GetCapturedFields(
            MethodDefinition lambda,
            List<DiagnosticMessage> diagnostics,
            MethodDefinition owner,
            Instruction diagnosticInstruction)
        {
            if (lambda.IsStatic)
            {
                return Array.Empty<FieldDefinition>();
            }

            var declaringType = lambda.DeclaringType;
            if (!IsCompilerGeneratedLambdaContainer(declaringType))
            {
                if (IsUnmanaged(declaringType))
                {
                    return Array.Empty<FieldDefinition>();
                }

                AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate target '{lambda.FullName}' is not a compiler-generated lambda.");
                return null;
            }

            var fields = declaringType.Fields
                .Where(f => !f.IsStatic)
                .ToArray();

            foreach (var field in fields)
            {
                if (!IsUnmanaged(field.FieldType))
                {
                    AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate capture '{field.Name}' has managed type '{field.FieldType.FullName}'.");
                    return null;
                }
            }

            return fields;
        }

        private static bool IsCompilerGeneratedLambdaContainer(TypeDefinition type)
        {
            return type.Name.StartsWith("<>c", StringComparison.Ordinal);
        }

        private static bool ValidateLambdaBodyUsesOnlyUnmanagedTypes(
            MethodDefinition lambda,
            IReadOnlyList<FieldDefinition> capturedFields,
            List<DiagnosticMessage> diagnostics,
            MethodDefinition owner,
            Instruction diagnosticInstruction)
        {
            foreach (var parameter in lambda.Parameters)
            {
                if (!IsUnmanaged(parameter.ParameterType))
                {
                    AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate parameter '{parameter.Name}' has managed type '{parameter.ParameterType.FullName}'.");
                    return false;
                }
            }

            if (lambda.ReturnType.MetadataType != MetadataType.Void &&
                !IsUnmanaged(lambda.ReturnType))
            {
                AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate return type '{lambda.ReturnType.FullName}' is managed.");
                return false;
            }

            foreach (var variable in lambda.Body.Variables)
            {
                if (!IsUnmanaged(variable.VariableType))
                {
                    AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate local '{lambda.Body.Variables.IndexOf(variable)}' has managed type '{variable.VariableType.FullName}'.");
                    return false;
                }
            }

            foreach (var handler in lambda.Body.ExceptionHandlers)
            {
                if (handler.CatchType != null)
                {
                    AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate body cannot catch managed exception type '{handler.CatchType.FullName}'.");
                    return false;
                }
            }

            var capturedFieldNames = new HashSet<string>(capturedFields.Select(field => field.FullName));
            foreach (var instruction in lambda.Body.Instructions)
            {
                if (TryGetManagedTypeUsage(instruction, capturedFieldNames, out var message))
                {
                    AddError(diagnostics, lambda, instruction, owner, diagnosticInstruction, message);
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetManagedTypeUsage(
            Instruction instruction,
            ISet<string> capturedFieldNames,
            out string message)
        {
            message = null;

            switch (instruction.OpCode.Code)
            {
                case Code.Ldstr:
                    message = "NativeLinq delegate body cannot use string literals.";
                    return true;
                case Code.Newarr:
                    if (instruction.Operand is TypeReference arrayElementType)
                    {
                        message = $"NativeLinq delegate body cannot create managed array '{arrayElementType.FullName}[]'.";
                    }
                    else
                    {
                        message = "NativeLinq delegate body cannot create managed arrays.";
                    }

                    return true;
                case Code.Box:
                    if (instruction.Operand is TypeReference boxedType)
                    {
                        message = $"NativeLinq delegate body cannot box '{boxedType.FullName}'.";
                    }
                    else
                    {
                        message = "NativeLinq delegate body cannot box values.";
                    }

                    return true;
            }

            if (instruction.OpCode == OpCodes.Newobj &&
                instruction.Operand is MethodReference constructor &&
                !IsUnmanaged(constructor.DeclaringType))
            {
                message = $"NativeLinq delegate body cannot create managed type '{constructor.DeclaringType.FullName}'.";
                return true;
            }

            if (instruction.Operand is FieldReference fieldReference)
            {
                var resolvedField = fieldReference.Resolve();
                var fieldName = resolvedField?.FullName ?? fieldReference.FullName;
                if (!capturedFieldNames.Contains(fieldName) &&
                    !IsUnmanaged(fieldReference.FieldType))
                {
                    message = $"NativeLinq delegate body cannot use managed field type '{fieldReference.FieldType.FullName}'.";
                    return true;
                }

                if (TryGetManagedGenericArgument(fieldReference.DeclaringType, out var managedDeclaringGenericArgument))
                {
                    message = $"NativeLinq delegate body cannot use managed generic type '{managedDeclaringGenericArgument.FullName}'.";
                    return true;
                }

                return false;
            }

            if (instruction.Operand is MethodReference methodReference)
            {
                if (methodReference.HasThis && !IsUnmanaged(methodReference.DeclaringType))
                {
                    message = $"NativeLinq delegate body cannot call instance method on managed type '{methodReference.DeclaringType.FullName}'.";
                    return true;
                }

                if (methodReference.ReturnType.MetadataType != MetadataType.Void &&
                    !IsUnmanaged(methodReference.ReturnType))
                {
                    message = $"NativeLinq delegate body cannot use managed return type '{methodReference.ReturnType.FullName}'.";
                    return true;
                }

                foreach (var parameter in methodReference.Parameters)
                {
                    if (!IsUnmanaged(parameter.ParameterType))
                    {
                        message = $"NativeLinq delegate body cannot call method '{methodReference.FullName}' because parameter type '{parameter.ParameterType.FullName}' is managed.";
                        return true;
                    }
                }

                if (methodReference is GenericInstanceMethod genericMethod)
                {
                    foreach (var genericArgument in genericMethod.GenericArguments)
                    {
                        if (!IsUnmanaged(genericArgument))
                        {
                            message = $"NativeLinq delegate body cannot use managed generic argument '{genericArgument.FullName}'.";
                            return true;
                        }
                    }
                }

                if (TryGetManagedGenericArgument(methodReference.DeclaringType, out var managedMethodDeclaringGenericArgument))
                {
                    message = $"NativeLinq delegate body cannot use managed generic type '{managedMethodDeclaringGenericArgument.FullName}'.";
                    return true;
                }

                return false;
            }

            if (instruction.Operand is CallSite callSite)
            {
                if (callSite.ReturnType.MetadataType != MetadataType.Void &&
                    !IsUnmanaged(callSite.ReturnType))
                {
                    message = $"NativeLinq delegate body cannot use managed return type '{callSite.ReturnType.FullName}'.";
                    return true;
                }

                foreach (var parameter in callSite.Parameters)
                {
                    if (!IsUnmanaged(parameter.ParameterType))
                    {
                        message = $"NativeLinq delegate body cannot use managed calli parameter type '{parameter.ParameterType.FullName}'.";
                        return true;
                    }
                }

                return false;
            }

            if (instruction.Operand is TypeReference typeReference &&
                IsManagedTypeOperand(instruction.OpCode.Code, typeReference))
            {
                message = $"NativeLinq delegate body cannot use managed type '{typeReference.FullName}'.";
                return true;
            }

            return false;
        }

        private static bool IsManagedTypeOperand(Code opcode, TypeReference typeReference)
        {
            switch (opcode)
            {
                case Code.Castclass:
                case Code.Isinst:
                case Code.Ldtoken:
                case Code.Unbox:
                case Code.Unbox_Any:
                case Code.Cpobj:
                case Code.Initobj:
                case Code.Ldobj:
                case Code.Stobj:
                case Code.Mkrefany:
                    return !IsUnmanaged(typeReference);
                default:
                    return false;
            }
        }

        private static bool TryGetManagedGenericArgument(TypeReference type, out TypeReference managedType)
        {
            managedType = null;
            switch (type)
            {
                case null:
                    return false;
                case GenericInstanceType genericInstance:
                    foreach (var genericArgument in genericInstance.GenericArguments)
                    {
                        if (!IsUnmanaged(genericArgument))
                        {
                            managedType = genericArgument;
                            return true;
                        }

                        if (TryGetManagedGenericArgument(genericArgument, out managedType))
                        {
                            return true;
                        }
                    }

                    return false;
                case ByReferenceType byReference:
                    return TryGetManagedGenericArgument(byReference.ElementType, out managedType);
                case PointerType pointer:
                    return TryGetManagedGenericArgument(pointer.ElementType, out managedType);
                case RequiredModifierType requiredModifier:
                    return TryGetManagedGenericArgument(requiredModifier.ElementType, out managedType);
                case OptionalModifierType optionalModifier:
                    return TryGetManagedGenericArgument(optionalModifier.ElementType, out managedType);
                default:
                    return false;
            }
        }

        private static bool IsUnmanaged(TypeReference type)
        {
            return IsUnmanaged(type, new HashSet<string>());
        }

        private static bool IsUnmanaged(TypeReference type, ISet<string> visited)
        {
            switch (type)
            {
                case null:
                    return false;
                case RequiredModifierType requiredModifier:
                    return IsUnmanaged(requiredModifier.ElementType, visited);
                case OptionalModifierType optionalModifier:
                    return IsUnmanaged(optionalModifier.ElementType, visited);
                case ByReferenceType byReference:
                    return IsUnmanaged(byReference.ElementType, visited);
                case PointerType:
                    return true;
                case ArrayType:
                    return false;
                case GenericParameter genericParameter:
                    return genericParameter.HasNotNullableValueTypeConstraint;
            }

            type = type.GetElementType();
            if (type.MetadataType == MetadataType.Void || type.IsPrimitive)
            {
                return true;
            }

            if (type.MetadataType == MetadataType.IntPtr || type.MetadataType == MetadataType.UIntPtr)
            {
                return true;
            }

            var definition = type.Resolve();
            if (definition == null || !definition.IsValueType)
            {
                return false;
            }

            if (definition.IsEnum)
            {
                return true;
            }

            var visitKey = type.FullName;
            if (!visited.Add(visitKey))
            {
                return true;
            }

            try
            {
                return definition.Fields
                    .Where(field => !field.IsStatic)
                    .All(field => IsUnmanaged(CloseTypeGenericType(type, field.FieldType), visited));
            }
            finally
            {
                visited.Remove(visitKey);
            }
        }

        private static TypeReference CloseTypeGenericType(TypeReference declaringType, TypeReference fieldType)
        {
            var declaringInstance = declaringType as GenericInstanceType;
            return RewriteTypeReference(
                fieldType,
                genericParameter => genericParameter.Type == GenericParameterType.Type && declaringInstance != null
                    ? declaringInstance.GenericArguments[genericParameter.Position]
                    : null,
                typeReference => typeReference);
        }
    }
}
