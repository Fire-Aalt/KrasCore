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
        private bool TryRewriteNativeDelegateCall(
            MethodDefinition owner,
            Instruction callInstruction,
            GenericInstanceMethod placeholderCall,
            List<DiagnosticMessage> diagnostics)
        {
            var placeholder = placeholderCall.Resolve();
            if (placeholder == null)
            {
                return false;
            }

            var interfaceDefinitions = GetNativeDelegateInterfaceDefinitions(placeholder).ToArray();
            if (interfaceDefinitions.Length == 0)
            {
                return false;
            }

            var module = owner.Module;
            var delegateParameters = placeholder.Parameters
                .Where(parameter => IsDelegateType(CloseMethodGenericType(module, parameter.ParameterType, placeholderCall)))
                .ToArray();

            if (delegateParameters.Length != interfaceDefinitions.Length)
            {
                AddError(diagnostics, owner, callInstruction, $"NativeLinq delegate method '{placeholder.FullName}' has {interfaceDefinitions.Length} delegate attributes but {delegateParameters.Length} delegate parameters.");
                return false;
            }

            var adapters = new Dictionary<int, AdapterInfo>();
            for (var i = delegateParameters.Length - 1; i >= 0; i--)
            {
                var parameter = delegateParameters[i];
                var trailingArguments = MoveTrailingArgumentsAfterDelegate(owner, callInstruction, placeholder, parameter, diagnostics);
                if (trailingArguments == null)
                {
                    return false;
                }

                var delegateType = CloseMethodGenericType(module, parameter.ParameterType, placeholderCall);
                var signature = ResolveDelegateSignature(module, delegateType, diagnostics, owner, callInstruction);
                if (signature == null)
                {
                    return false;
                }

                var interfaceType = CreateNativeDelegateInterfaceType(module, interfaceDefinitions[i], signature);
                var adapter = CreateAdapter(owner, callInstruction, signature, interfaceType, diagnostics);
                if (adapter == null)
                {
                    return false;
                }

                foreach (var trailingArgument in trailingArguments)
                {
                    owner.Body.GetILProcessor().InsertBefore(callInstruction, trailingArgument);
                }

                adapters.Add(parameter.Index, adapter);
            }

            var target = FindTargetMethod(module, placeholderCall, placeholder, adapters, diagnostics, owner, callInstruction);
            if (target == null)
            {
                return false;
            }

            callInstruction.Operand = target.Call;
            MapRewrittenEnumerator(
                CloseMethodGenericType(module, placeholderCall.ReturnType, placeholderCall),
                target.ReturnType);

            return true;
        }

        private static IEnumerable<TypeReference> GetNativeDelegateInterfaceDefinitions(MethodDefinition method)
        {
            foreach (var attribute in method.CustomAttributes)
            {
                if (attribute.AttributeType.FullName != NativeDelegateMethodAttributeTypeName ||
                    attribute.ConstructorArguments.Count != 1)
                {
                    continue;
                }

                var argumentValue = attribute.ConstructorArguments[0].Value;
                if (argumentValue is TypeReference interfaceType)
                {
                    yield return interfaceType;
                }
                else if (argumentValue is CustomAttributeArgument[] interfaceTypes)
                {
                    foreach (var interfaceTypeArgument in interfaceTypes)
                    {
                        if (interfaceTypeArgument.Value is TypeReference arrayInterfaceType)
                        {
                            yield return arrayInterfaceType;
                        }
                    }
                }
            }
        }

        private static bool HasNativeDelegateMethodAttribute(MethodDefinition method)
        {
            return method.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName == NativeDelegateMethodAttributeTypeName);
        }

        private static bool IsDelegateType(TypeReference type)
        {
            var definition = type.Resolve();
            while (definition != null)
            {
                if (definition.FullName == "System.MulticastDelegate")
                {
                    return true;
                }

                definition = definition.BaseType?.Resolve();
            }

            return false;
        }

        private static IReadOnlyList<Instruction> MoveTrailingArgumentsAfterDelegate(
            MethodDefinition owner,
            Instruction callInstruction,
            MethodDefinition placeholder,
            ParameterDefinition delegateParameter,
            List<DiagnosticMessage> diagnostics)
        {
            var trailingCount = placeholder.Parameters.Count - delegateParameter.Index - 1;
            if (trailingCount == 0)
            {
                return Array.Empty<Instruction>();
            }

            var moved = new List<Instruction>[trailingCount];
            var current = PreviousMeaningful(callInstruction);
            for (var i = trailingCount - 1; i >= 0; i--)
            {
                var producerStart = FindStackProducerStart(current, 1);
                if (producerStart == null)
                {
                    AddError(diagnostics, owner, callInstruction, "NativeLinq delegate weaving only supports simple trailing arguments after the delegate parameter.");
                    return null;
                }

                var beforeProducer = PreviousMeaningful(producerStart);
                moved[i] = new List<Instruction>();
                foreach (var instruction in GetMeaningfulInstructionRange(producerStart, current))
                {
                    if (!CanMoveTrailingArgumentInstruction(instruction))
                    {
                        AddError(diagnostics, owner, callInstruction, "NativeLinq delegate weaving only supports simple trailing arguments after the delegate parameter.");
                        return null;
                    }

                    moved[i].Add(CloneSimpleInstruction(instruction));
                    instruction.OpCode = OpCodes.Nop;
                    instruction.Operand = null;
                }

                current = beforeProducer;
            }

            return moved.SelectMany(argument => argument).ToArray();
        }

        private static IEnumerable<Instruction> GetMeaningfulInstructionRange(Instruction start, Instruction end)
        {
            var current = start;
            while (current != null)
            {
                if (current.OpCode != OpCodes.Nop)
                {
                    yield return current;
                }

                if (current == end)
                {
                    yield break;
                }

                current = current.Next;
            }
        }

        private static bool CanMoveTrailingArgumentInstruction(Instruction instruction)
        {
            return instruction.OpCode.FlowControl == FlowControl.Next ||
                instruction.OpCode.FlowControl == FlowControl.Call;
        }

        private static Instruction CloneSimpleInstruction(Instruction instruction)
        {
            switch (instruction.Operand)
            {
                case null:
                    return Instruction.Create(instruction.OpCode);
                case sbyte value:
                    return Instruction.Create(instruction.OpCode, value);
                case byte value:
                    return Instruction.Create(instruction.OpCode, value);
                case int value:
                    return Instruction.Create(instruction.OpCode, value);
                case long value:
                    return Instruction.Create(instruction.OpCode, value);
                case float value:
                    return Instruction.Create(instruction.OpCode, value);
                case double value:
                    return Instruction.Create(instruction.OpCode, value);
                case string value:
                    return Instruction.Create(instruction.OpCode, value);
                case TypeReference value:
                    return Instruction.Create(instruction.OpCode, value);
                case MethodReference value:
                    return Instruction.Create(instruction.OpCode, value);
                case FieldReference value:
                    return Instruction.Create(instruction.OpCode, value);
                case ParameterDefinition value:
                    return Instruction.Create(instruction.OpCode, value);
                case VariableDefinition value:
                    return Instruction.Create(instruction.OpCode, value);
                case Instruction value:
                    return Instruction.Create(instruction.OpCode, value);
                case Instruction[] value:
                    return Instruction.Create(instruction.OpCode, value);
                default:
                    throw new InvalidOperationException($"Unsupported instruction operand '{instruction.Operand.GetType().FullName}'.");
            }
        }

        private DelegateSignature ResolveDelegateSignature(
            ModuleDefinition module,
            TypeReference delegateType,
            List<DiagnosticMessage> diagnostics,
            MethodDefinition owner,
            Instruction diagnosticInstruction)
        {
            var delegateDefinition = delegateType.Resolve();
            var invoke = delegateDefinition?.Methods.FirstOrDefault(method => method.Name == "Invoke");
            if (invoke == null)
            {
                AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate type '{delegateType.FullName}' does not have an Invoke method.");
                return null;
            }

            return new DelegateSignature(
                invoke.Parameters
                    .Select(parameter => CloseDelegateType(module, parameter.ParameterType, delegateType))
                    .ToArray(),
                CloseDelegateType(module, invoke.ReturnType, delegateType));
        }

        private static TypeReference CloseDelegateType(ModuleDefinition module, TypeReference type, TypeReference delegateType)
        {
            var delegateInstance = delegateType as GenericInstanceType;
            return RewriteTypeReference(
                type,
                genericParameter => genericParameter.Type == GenericParameterType.Type && delegateInstance != null
                    ? module.ImportReference(delegateInstance.GenericArguments[genericParameter.Position])
                    : null,
                module.ImportReference);
        }

        private static TypeReference CreateNativeDelegateInterfaceType(
            ModuleDefinition module,
            TypeReference interfaceDefinition,
            DelegateSignature signature)
        {
            var importedDefinition = module.ImportReference(interfaceDefinition);
            var genericParameterCount = importedDefinition.GenericParameters.Count;
            var arityMarker = importedDefinition.Name.LastIndexOf('`');
            if (genericParameterCount == 0 &&
                arityMarker >= 0 &&
                int.TryParse(importedDefinition.Name.Substring(arityMarker + 1), out var arity))
            {
                genericParameterCount = arity;
            }

            if (genericParameterCount == 0)
            {
                return importedDefinition;
            }

            var interfaceType = new GenericInstanceType(importedDefinition);
            foreach (var parameterType in signature.ParameterTypes)
            {
                interfaceType.GenericArguments.Add(module.ImportReference(parameterType.GetElementType()));
            }

            if (interfaceType.GenericArguments.Count < genericParameterCount &&
                signature.ReturnType.MetadataType != MetadataType.Void)
            {
                interfaceType.GenericArguments.Add(module.ImportReference(signature.ReturnType));
            }

            return interfaceType;
        }

        private TargetMethodInfo FindTargetMethod(
            ModuleDefinition module,
            GenericInstanceMethod placeholderCall,
            MethodDefinition placeholder,
            IReadOnlyDictionary<int, AdapterInfo> adapters,
            List<DiagnosticMessage> diagnostics,
            MethodDefinition owner,
            Instruction diagnosticInstruction)
        {
            var placeholderGenericArguments = new Dictionary<string, TypeReference>();
            for (var i = 0; i < placeholder.GenericParameters.Count; i++)
            {
                placeholderGenericArguments[placeholder.GenericParameters[i].Name] =
                    ResolveRewrittenType(module, placeholderCall.GenericArguments[i]);
            }

            var candidates = placeholder.DeclaringType.Resolve().Methods
                .Where(method =>
                    method.Name == placeholder.Name &&
                    !HasNativeDelegateMethodAttribute(method) &&
                    method.Parameters.Count == placeholder.Parameters.Count)
                .ToArray();

            foreach (var candidate in candidates)
            {
                if (TryCreateTargetMethod(module, placeholderCall, placeholder, candidate, placeholderGenericArguments, adapters, out var target))
                {
                    return target;
                }
            }

            AddError(diagnostics, owner, diagnosticInstruction, $"NativeLinq delegate weaving could not find unmanaged overload for '{placeholder.FullName}'.");
            return null;
        }

        private bool TryCreateTargetMethod(
            ModuleDefinition module,
            GenericInstanceMethod placeholderCall,
            MethodDefinition placeholder,
            MethodDefinition candidate,
            IReadOnlyDictionary<string, TypeReference> placeholderGenericArguments,
            IReadOnlyDictionary<int, AdapterInfo> adapters,
            out TargetMethodInfo target)
        {
            target = null;
            var targetGenericArguments = new TypeReference[candidate.GenericParameters.Count];

            foreach (var adapter in adapters)
            {
                if (!TryAssignAdapterGenericArgument(candidate.Parameters[adapter.Key].ParameterType, adapter.Value, targetGenericArguments))
                {
                    return false;
                }
            }

            for (var i = 0; i < candidate.GenericParameters.Count; i++)
            {
                if (targetGenericArguments[i] != null)
                {
                    continue;
                }

                var genericParameter = candidate.GenericParameters[i];
                var matchingAdapters = adapters.Values
                    .Where(value => GenericParameterAcceptsInterface(genericParameter, value.InterfaceType))
                    .ToArray();
                if (matchingAdapters.Length == 1)
                {
                    targetGenericArguments[i] = matchingAdapters[0].AdapterType;
                }
                else if (placeholderGenericArguments.TryGetValue(genericParameter.Name, out var argument))
                {
                    targetGenericArguments[i] = argument;
                }
                else
                {
                    return false;
                }
            }

            foreach (var adapter in adapters)
            {
                if (!AdapterSatisfiesCandidateParameter(
                    module,
                    candidate.Parameters[adapter.Key].ParameterType,
                    candidate,
                    targetGenericArguments,
                    adapter.Value))
                {
                    return false;
                }
            }

            for (var i = 0; i < candidate.Parameters.Count; i++)
            {
                if (adapters.ContainsKey(i))
                {
                    continue;
                }

                var parameterType = SubstituteMethodGenericArguments(module, candidate.Parameters[i].ParameterType, candidate, targetGenericArguments);
                var placeholderParameterType = ResolveRewrittenType(
                    module,
                    CloseMethodGenericType(module, placeholder.Parameters[i].ParameterType, placeholderCall));
                if (parameterType.ContainsGenericParameter ||
                    !TypeReferencesMatch(parameterType, placeholderParameterType))
                {
                    return false;
                }
            }

            var methodReference = CreateMethodReference(module, candidate);
            var genericMethod = new GenericInstanceMethod(methodReference);
            foreach (var argument in targetGenericArguments)
            {
                genericMethod.GenericArguments.Add(module.ImportReference(argument));
            }

            target = new TargetMethodInfo(
                genericMethod,
                SubstituteMethodGenericArguments(module, candidate.ReturnType, candidate, targetGenericArguments));
            return true;
        }

        private static bool TryAssignAdapterGenericArgument(
            TypeReference parameterType,
            AdapterInfo adapter,
            TypeReference[] targetGenericArguments)
        {
            if (parameterType is GenericParameter genericParameter &&
                genericParameter.Type == GenericParameterType.Method)
            {
                if (!GenericParameterAcceptsInterface(genericParameter, adapter.InterfaceType))
                {
                    return false;
                }

                var existing = targetGenericArguments[genericParameter.Position];
                if (existing != null && !TypeReferencesMatch(existing, adapter.AdapterType))
                {
                    return false;
                }

                targetGenericArguments[genericParameter.Position] = adapter.AdapterType;
                return true;
            }

            if (parameterType is GenericInstanceType genericInstance)
            {
                foreach (var argument in genericInstance.GenericArguments)
                {
                    if (TryAssignAdapterGenericArgument(argument, adapter, targetGenericArguments))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool AdapterSatisfiesCandidateParameter(
            ModuleDefinition module,
            TypeReference parameterType,
            MethodDefinition candidate,
            IReadOnlyList<TypeReference> targetGenericArguments,
            AdapterInfo adapter)
        {
            if (parameterType is GenericParameter genericParameter &&
                genericParameter.Type == GenericParameterType.Method)
            {
                var candidateGenericParameter = candidate.GenericParameters[genericParameter.Position];
                return candidateGenericParameter.Constraints.Any(constraint =>
                    TypeReferencesMatch(
                        SubstituteMethodGenericArguments(module, constraint.ConstraintType, candidate, targetGenericArguments),
                        adapter.InterfaceType));
            }

            if (parameterType is GenericInstanceType genericInstance)
            {
                foreach (var argument in genericInstance.GenericArguments)
                {
                    if (AdapterSatisfiesCandidateParameter(module, argument, candidate, targetGenericArguments, adapter))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TypeReferencesMatch(TypeReference left, TypeReference right)
        {
            if (left is GenericInstanceType leftGeneric && right is GenericInstanceType rightGeneric)
            {
                return leftGeneric.ElementType.FullName == rightGeneric.ElementType.FullName &&
                    leftGeneric.GenericArguments.Count == rightGeneric.GenericArguments.Count &&
                    leftGeneric.GenericArguments.Zip(rightGeneric.GenericArguments, TypeReferencesMatch).All(match => match);
            }

            return left.FullName == right.FullName;
        }

        private static bool GenericParameterAcceptsInterface(GenericParameter genericParameter, TypeReference interfaceType)
        {
            return genericParameter.Constraints.Any(constraint =>
                TypeDefinitionsMatch(constraint.ConstraintType, interfaceType));
        }

        private static bool TypeDefinitionsMatch(TypeReference left, TypeReference right)
        {
            var leftElement = left.GetElementType();
            var rightElement = right.GetElementType();
            return leftElement.FullName == rightElement.FullName;
        }

        private static MethodReference CreateMethodReference(ModuleDefinition module, MethodDefinition methodDefinition)
        {
            var methodReference = new MethodReference(
                methodDefinition.Name,
                module.TypeSystem.Void,
                module.ImportReference(methodDefinition.DeclaringType))
            {
                HasThis = methodDefinition.HasThis,
                ExplicitThis = methodDefinition.ExplicitThis,
                CallingConvention = methodDefinition.CallingConvention,
            };

            foreach (var genericParameter in methodDefinition.GenericParameters)
            {
                methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
            }

            methodReference.ReturnType = ImportMethodReferenceSignatureType(module, methodDefinition.ReturnType, methodReference);
            foreach (var parameter in methodDefinition.Parameters)
            {
                methodReference.Parameters.Add(new ParameterDefinition(
                    ImportMethodReferenceSignatureType(module, parameter.ParameterType, methodReference)));
            }

            return methodReference;
        }

        private static TypeReference ImportMethodReferenceSignatureType(
            ModuleDefinition module,
            TypeReference type,
            MethodReference methodGenericOwner)
        {
            return RewriteTypeReference(
                type,
                genericParameter => genericParameter.Type == GenericParameterType.Method
                    ? methodGenericOwner.GenericParameters[genericParameter.Position]
                    : null,
                module.ImportReference);
        }

        private static TypeReference SubstituteMethodGenericArguments(
            ModuleDefinition module,
            TypeReference type,
            MethodDefinition method,
            IReadOnlyList<TypeReference> genericArguments)
        {
            return RewriteTypeReference(
                type,
                genericParameter => genericParameter.Type == GenericParameterType.Method
                    ? module.ImportReference(genericArguments[genericParameter.Position])
                    : null,
                module.ImportReference);
        }

        private static TypeReference CloseMethodGenericType(
            ModuleDefinition module,
            TypeReference type,
            GenericInstanceMethod method)
        {
            return RewriteTypeReference(
                type,
                genericParameter => genericParameter.Type == GenericParameterType.Method
                    ? module.ImportReference(method.GenericArguments[genericParameter.Position])
                    : null,
                module.ImportReference);
        }

        private void MapRewrittenEnumerator(TypeReference placeholderReturnType, TypeReference realReturnType)
        {
            if (placeholderReturnType is not GenericInstanceType placeholderQueryType ||
                placeholderQueryType.GenericArguments.Count != 2 ||
                realReturnType is not GenericInstanceType realQueryType ||
                realQueryType.GenericArguments.Count != 2)
            {
                return;
            }

            _rewrittenEnumeratorTypes[placeholderQueryType.GenericArguments[1].FullName] = realQueryType.GenericArguments[1];
        }

        private TypeReference ResolveRewrittenType(ModuleDefinition module, TypeReference type)
        {
            if (_rewrittenEnumeratorTypes.TryGetValue(type.FullName, out var rewritten))
            {
                return module.ImportReference(rewritten);
            }

            if (type is GenericInstanceType genericInstance)
            {
                var closed = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                foreach (var argument in genericInstance.GenericArguments)
                {
                    closed.GenericArguments.Add(ResolveRewrittenType(module, argument));
                }

                return closed;
            }

            return module.ImportReference(type);
        }
    }
}
