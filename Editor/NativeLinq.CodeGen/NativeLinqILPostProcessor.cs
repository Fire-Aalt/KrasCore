namespace KrasCore.NativeLinq.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Unity.CompilationPipeline.Common.Diagnostics;
    using Unity.CompilationPipeline.Common.ILPostProcessing;

    internal sealed class NativeLinqILPostProcessor : ILPostProcessor
    {
        private const string KrasCoreAssemblyName = "KrasCore";
        private const string NativeDelegateMethodAttributeTypeName = "KrasCore.NativeDelegateMethodAttribute";

        private int adapterIndex;
        private readonly Dictionary<string, TypeReference> rewrittenEnumeratorTypes = new Dictionary<string, TypeReference>();
        private readonly Dictionary<string, IReadOnlyDictionary<FieldDefinition, VariableDefinition>> rewrittenCaptureLocals =
            new Dictionary<string, IReadOnlyDictionary<FieldDefinition, VariableDefinition>>();

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly.InMemoryAssembly.PeData == null || compiledAssembly.InMemoryAssembly.PdbData == null)
            {
                return false;
            }

            return compiledAssembly.Name == KrasCoreAssemblyName ||
                compiledAssembly.References.Any(r => Path.GetFileNameWithoutExtension(r) == KrasCoreAssemblyName);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();
            var assembly = AssemblyDefinitionFor(compiledAssembly);
            var modified = false;

            foreach (var type in assembly.MainModule.Types.ToArray())
            {
                modified |= ProcessType(type, diagnostics);
            }

            if (!modified)
            {
                return new ILPostProcessResult(null, diagnostics);
            }

            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            assembly.Write(pe, new WriterParameters
            {
                WriteSymbols = true,
                SymbolStream = pdb,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
            });

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), diagnostics);
        }

        private bool ProcessType(TypeDefinition type, List<DiagnosticMessage> diagnostics)
        {
            var modified = false;

            foreach (var method in type.Methods)
            {
                if (method.HasBody)
                {
                    modified |= ProcessMethod(method, diagnostics);
                }
            }

            foreach (var nestedType in type.NestedTypes)
            {
                modified |= ProcessType(nestedType, diagnostics);
            }

            return modified;
        }

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
                AddError(diagnostics, owner, $"NativeLinq delegate method '{placeholder.FullName}' has {interfaceDefinitions.Length} delegate attributes but {delegateParameters.Length} delegate parameters.");
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
                var signature = ResolveDelegateSignature(module, delegateType, diagnostics, owner);
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

            var target = FindTargetMethod(module, placeholderCall, placeholder, adapters, diagnostics, owner);
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
                    attribute.ConstructorArguments.Count != 1 ||
                    attribute.ConstructorArguments[0].Value is not TypeReference interfaceType)
                {
                    continue;
                }

                yield return interfaceType;
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

            var moved = new Instruction[trailingCount];
            var current = callInstruction;
            for (var i = trailingCount - 1; i >= 0; i--)
            {
                var producer = PreviousMeaningful(current);
                if (producer == null || producer.OpCode.FlowControl != FlowControl.Next)
                {
                    AddError(diagnostics, owner, "NativeLinq delegate weaving only supports simple trailing arguments after the delegate parameter.");
                    return null;
                }

                moved[i] = CloneSimpleInstruction(producer);
                producer.OpCode = OpCodes.Nop;
                producer.Operand = null;
                current = producer;
            }

            return moved;
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
            MethodDefinition owner)
        {
            var delegateDefinition = delegateType.Resolve();
            var invoke = delegateDefinition?.Methods.FirstOrDefault(method => method.Name == "Invoke");
            if (invoke == null)
            {
                AddError(diagnostics, owner, $"NativeLinq delegate type '{delegateType.FullName}' does not have an Invoke method.");
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
            switch (type)
            {
                case GenericParameter genericParameter when genericParameter.Type == GenericParameterType.Type && delegateInstance != null:
                    return module.ImportReference(delegateInstance.GenericArguments[genericParameter.Position]);
                case GenericInstanceType genericInstance:
                    var closedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        closedInstance.GenericArguments.Add(CloseDelegateType(module, argument, delegateType));
                    }

                    return closedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(CloseDelegateType(module, byReference.ElementType, delegateType));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        CloseDelegateType(module, requiredModifier.ElementType, delegateType));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        CloseDelegateType(module, optionalModifier.ElementType, delegateType));
                default:
                    return module.ImportReference(type);
            }
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
            MethodDefinition owner)
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

            AddError(diagnostics, owner, $"NativeLinq delegate weaving could not find unmanaged overload for '{placeholder.FullName}'.");
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

            for (var i = 0; i < candidate.GenericParameters.Count; i++)
            {
                var genericParameter = candidate.GenericParameters[i];
                var adapter = adapters.Values.FirstOrDefault(value => GenericParameterAcceptsInterface(genericParameter, value.InterfaceType));
                if (adapter != null)
                {
                    targetGenericArguments[i] = adapter.AdapterType;
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
            switch (type)
            {
                case GenericParameter genericParameter when genericParameter.Type == GenericParameterType.Method:
                    return methodGenericOwner.GenericParameters[genericParameter.Position];
                case GenericInstanceType genericInstance:
                    var importedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        importedInstance.GenericArguments.Add(ImportMethodReferenceSignatureType(module, argument, methodGenericOwner));
                    }

                    return importedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(ImportMethodReferenceSignatureType(module, byReference.ElementType, methodGenericOwner));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        ImportMethodReferenceSignatureType(module, requiredModifier.ElementType, methodGenericOwner));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        ImportMethodReferenceSignatureType(module, optionalModifier.ElementType, methodGenericOwner));
                default:
                    return module.ImportReference(type);
            }
        }

        private static TypeReference SubstituteMethodGenericArguments(
            ModuleDefinition module,
            TypeReference type,
            MethodDefinition method,
            IReadOnlyList<TypeReference> genericArguments)
        {
            switch (type)
            {
                case GenericParameter genericParameter when genericParameter.Type == GenericParameterType.Method:
                    return module.ImportReference(genericArguments[genericParameter.Position]);
                case GenericInstanceType genericInstance:
                    var closedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        closedInstance.GenericArguments.Add(SubstituteMethodGenericArguments(module, argument, method, genericArguments));
                    }

                    return closedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(SubstituteMethodGenericArguments(module, byReference.ElementType, method, genericArguments));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        SubstituteMethodGenericArguments(module, requiredModifier.ElementType, method, genericArguments));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        SubstituteMethodGenericArguments(module, optionalModifier.ElementType, method, genericArguments));
                default:
                    return module.ImportReference(type);
            }
        }

        private bool ProcessMethod(MethodDefinition method, List<DiagnosticMessage> diagnostics)
        {
            var modified = false;
            rewrittenEnumeratorTypes.Clear();
            rewrittenCaptureLocals.Clear();
            var instructions = method.Body.Instructions;

            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if (instruction.OpCode != OpCodes.Call || instruction.Operand is not MethodReference call)
                {
                    continue;
                }

                if (call is not GenericInstanceMethod genericCall)
                {
                    continue;
                }

                modified |= TryRewriteNativeDelegateCall(method, instruction, genericCall, diagnostics);
            }

            if (modified)
            {
                method.Body.OptimizeMacros();
            }

            return modified;
        }

        private static TypeReference CloseMethodGenericType(
            ModuleDefinition module,
            TypeReference type,
            GenericInstanceMethod method)
        {
            switch (type)
            {
                case GenericParameter genericParameter when genericParameter.Type == GenericParameterType.Method:
                    return module.ImportReference(method.GenericArguments[genericParameter.Position]);
                case GenericInstanceType genericInstance:
                    var closedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        closedInstance.GenericArguments.Add(CloseMethodGenericType(module, argument, method));
                    }

                    return closedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(CloseMethodGenericType(module, byReference.ElementType, method));
                case PointerType pointer:
                    return new PointerType(CloseMethodGenericType(module, pointer.ElementType, method));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        CloseMethodGenericType(module, requiredModifier.ElementType, method));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        CloseMethodGenericType(module, optionalModifier.ElementType, method));
                default:
                    return module.ImportReference(type);
            }
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

            rewrittenEnumeratorTypes[placeholderQueryType.GenericArguments[1].FullName] = realQueryType.GenericArguments[1];
        }

        private TypeReference ResolveRewrittenType(ModuleDefinition module, TypeReference type)
        {
            if (rewrittenEnumeratorTypes.TryGetValue(type.FullName, out var rewritten))
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

        private AdapterInfo CreateAdapter(
            MethodDefinition owner,
            Instruction callInstruction,
            DelegateSignature signature,
            TypeReference interfaceType,
            List<DiagnosticMessage> diagnostics)
        {
            var instructionsToRemove = new List<Instruction>();
            var delegateCtorInstruction = PreviousMeaningful(callInstruction);
            if (delegateCtorInstruction?.OpCode != OpCodes.Newobj ||
                delegateCtorInstruction.Operand is not MethodReference delegateCtor ||
                delegateCtor.Parameters.Count != 2)
            {
                delegateCtorInstruction = TryGetCachedStaticLambdaCtor(callInstruction, instructionsToRemove);
                if (delegateCtorInstruction?.OpCode != OpCodes.Newobj ||
                    delegateCtorInstruction.Operand is not MethodReference cachedDelegateCtor ||
                    cachedDelegateCtor.Parameters.Count != 2)
                {
                    AddError(diagnostics, owner, "NativeLinq delegate weaving only supports direct lambda or compiler-cached static lambda delegate construction.");
                    return null;
                }
            }

            var functionInstruction = PreviousMeaningful(delegateCtorInstruction);
            if (functionInstruction?.OpCode != OpCodes.Ldftn ||
                functionInstruction.Operand is not MethodReference lambdaReference)
            {
                AddError(diagnostics, owner, "NativeLinq delegate weaving could not find the lambda method.");
                return null;
            }

            var lambda = lambdaReference.Resolve();
            if (lambda == null)
            {
                AddError(diagnostics, owner, $"NativeLinq delegate weaving could not resolve '{lambdaReference.FullName}'.");
                return null;
            }

            var targetInstruction = lambda.IsStatic ? null : PreviousMeaningful(functionInstruction);
            if (lambda.IsStatic)
            {
                var staticTargetInstruction = PreviousMeaningful(functionInstruction);
                if (staticTargetInstruction?.OpCode == OpCodes.Ldnull)
                {
                    instructionsToRemove.Add(staticTargetInstruction);
                }
            }

            var capturedFields = GetCapturedFields(lambda, diagnostics, owner);
            if (capturedFields == null)
            {
                return null;
            }

            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals = null;
            if (capturedFields.Count != 0 &&
                !TryRewriteClosureCaptures(owner, lambda.DeclaringType, targetInstruction, capturedFields, diagnostics, out captureLocals))
            {
                return null;
            }

            var module = owner.Module;
            var adapterType = new TypeDefinition(
                "KrasCore.Generated.NativeLinq",
                $"__NativeLinqDelegateAdapter_{adapterIndex++}",
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
                module.ImportReference(typeof(ValueType)));

            owner.DeclaringType.NestedTypes.Add(adapterType);

            var adapterTypeReference = module.ImportReference(adapterType);
            adapterType.Interfaces.Add(new InterfaceImplementation(interfaceType));

            var fieldMap = new Dictionary<FieldDefinition, FieldDefinition>();
            foreach (var capturedField in capturedFields)
            {
                var adapterField = new FieldDefinition(capturedField.Name, FieldAttributes.Private, module.ImportReference(capturedField.FieldType));
                adapterType.Fields.Add(adapterField);
                fieldMap.Add(capturedField, adapterField);
            }

            if (capturedFields.Count == 0)
            {
                adapterType.PackingSize = 0;
                adapterType.ClassSize = 1;
            }

            var ctor = CreateAdapterConstructor(module, adapterType, capturedFields, fieldMap);
            adapterType.Methods.Add(ctor);
            adapterType.Methods.Add(CloneLambdaMethod(module, adapterType, lambda, signature, interfaceType, fieldMap));

            foreach (var instruction in instructionsToRemove)
            {
                instruction.OpCode = OpCodes.Nop;
                instruction.Operand = null;
            }

            functionInstruction.OpCode = OpCodes.Nop;
            functionInstruction.Operand = null;
            delegateCtorInstruction.Operand = ctor;

            if (capturedFields.Count == 0 && targetInstruction != null)
            {
                targetInstruction.OpCode = OpCodes.Nop;
                targetInstruction.Operand = null;
            }
            else if (capturedFields.Count != 0)
            {
                RewriteAdapterConstructionArguments(owner, targetInstruction, capturedFields, captureLocals);
            }

            return new AdapterInfo(adapterTypeReference, interfaceType);
        }

        private static Instruction TryGetCachedStaticLambdaCtor(Instruction callInstruction, ICollection<Instruction> instructionsToRemove)
        {
            var storeCache = PreviousMeaningful(callInstruction);
            if (storeCache?.OpCode != OpCodes.Stsfld)
            {
                return null;
            }

            var duplicateForStore = PreviousMeaningful(storeCache);
            var delegateCtor = PreviousMeaningful(duplicateForStore);
            if (duplicateForStore?.OpCode != OpCodes.Dup || delegateCtor?.OpCode != OpCodes.Newobj)
            {
                return null;
            }

            var function = PreviousMeaningful(delegateCtor);
            var target = PreviousMeaningful(function);
            var popCachedMiss = PreviousMeaningful(target);
            var branchIfCached = PreviousMeaningful(popCachedMiss);
            var duplicateCachedValue = PreviousMeaningful(branchIfCached);
            var loadCache = PreviousMeaningful(duplicateCachedValue);

            if (function?.OpCode != OpCodes.Ldftn ||
                popCachedMiss?.OpCode != OpCodes.Pop ||
                duplicateCachedValue?.OpCode != OpCodes.Dup ||
                loadCache?.OpCode != OpCodes.Ldsfld ||
                branchIfCached == null ||
                branchIfCached.OpCode.FlowControl != FlowControl.Cond_Branch)
            {
                return null;
            }

            instructionsToRemove.Add(loadCache);
            instructionsToRemove.Add(duplicateCachedValue);
            instructionsToRemove.Add(branchIfCached);
            instructionsToRemove.Add(popCachedMiss);
            if (target?.OpCode == OpCodes.Ldnull)
            {
                instructionsToRemove.Add(target);
            }

            instructionsToRemove.Add(duplicateForStore);
            instructionsToRemove.Add(storeCache);
            return delegateCtor;
        }

        private bool TryRewriteClosureCaptures(
            MethodDefinition owner,
            TypeDefinition closureType,
            Instruction targetInstruction,
            IReadOnlyList<FieldDefinition> capturedFields,
            List<DiagnosticMessage> diagnostics,
            out IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals)
        {
            captureLocals = null;
            if (targetInstruction == null ||
                !TryGetLoadedLocal(owner, targetInstruction, out var closureLocal))
            {
                AddError(diagnostics, owner, "NativeLinq delegate weaving only supports local variable captures.");
                return false;
            }

            var key = $"{owner.FullName}|{owner.Body.Variables.IndexOf(closureLocal)}|{closureType.FullName}";
            if (rewrittenCaptureLocals.TryGetValue(key, out captureLocals))
            {
                return true;
            }

            var locals = new Dictionary<FieldDefinition, VariableDefinition>();
            foreach (var capturedField in capturedFields)
            {
                var local = new VariableDefinition(owner.Module.ImportReference(capturedField.FieldType));
                owner.Body.Variables.Add(local);
                locals.Add(capturedField, local);
            }

            owner.Body.InitLocals = true;
            if (!RewriteClosureLocalAccesses(owner, closureType, closureLocal, locals, diagnostics))
            {
                return false;
            }

            rewrittenCaptureLocals.Add(key, locals);
            captureLocals = locals;
            return true;
        }

        private static bool RewriteClosureLocalAccesses(
            MethodDefinition owner,
            TypeDefinition closureType,
            VariableDefinition closureLocal,
            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals,
            List<DiagnosticMessage> diagnostics)
        {
            foreach (var instruction in owner.Body.Instructions.ToArray())
            {
                if (instruction.OpCode == OpCodes.Newobj &&
                    instruction.Operand is MethodReference ctor &&
                    ctor.Name == ".ctor" &&
                    SameType(ctor.DeclaringType.Resolve(), closureType))
                {
                    var store = NextMeaningful(instruction);
                    if (store != null && IsStoreLocal(owner, store, closureLocal))
                    {
                        MakeNop(instruction);
                        MakeNop(store);
                    }
                }

                if (instruction.OpCode == OpCodes.Stfld &&
                    TryGetCaptureLocal(instruction.Operand, captureLocals, out _, out var storedLocal))
                {
                    var valueStart = FindStackProducerStart(PreviousMeaningful(instruction), 1);
                    var target = valueStart == null ? null : PreviousMeaningful(valueStart);
                    if (target == null || !IsLoadLocal(owner, target, closureLocal))
                    {
                        AddError(diagnostics, owner, "NativeLinq delegate weaving only supports direct captured local assignments.");
                        return false;
                    }

                    MakeNop(target);
                    instruction.OpCode = OpCodes.Stloc;
                    instruction.Operand = storedLocal;
                }
                else if (instruction.OpCode == OpCodes.Ldfld &&
                    TryGetCaptureLocal(instruction.Operand, captureLocals, out _, out var loadedLocal))
                {
                    var target = PreviousMeaningful(instruction);
                    if (target == null || !IsLoadLocal(owner, target, closureLocal))
                    {
                        AddError(diagnostics, owner, "NativeLinq delegate weaving only supports direct captured local reads.");
                        return false;
                    }

                    MakeNop(target);
                    instruction.OpCode = OpCodes.Ldloc;
                    instruction.Operand = loadedLocal;
                }
            }

            return true;
        }

        private static void RewriteAdapterConstructionArguments(
            MethodDefinition owner,
            Instruction targetInstruction,
            IReadOnlyList<FieldDefinition> capturedFields,
            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals)
        {
            var il = owner.Body.GetILProcessor();
            targetInstruction.OpCode = OpCodes.Ldloc;
            targetInstruction.Operand = captureLocals[capturedFields[0]];

            var current = targetInstruction;
            for (var i = 1; i < capturedFields.Count; i++)
            {
                var load = Instruction.Create(OpCodes.Ldloc, captureLocals[capturedFields[i]]);
                il.InsertAfter(current, load);
                current = load;
            }
        }

        private static MethodDefinition CreateAdapterConstructor(
            ModuleDefinition module,
            TypeDefinition adapterType,
            IReadOnlyList<FieldDefinition> capturedFields,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap)
        {
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            foreach (var capturedField in capturedFields)
            {
                ctor.Parameters.Add(new ParameterDefinition(
                    capturedField.Name,
                    Mono.Cecil.ParameterAttributes.None,
                    module.ImportReference(capturedField.FieldType)));
            }

            var il = ctor.Body.GetILProcessor();
            for (var i = 0; i < capturedFields.Count; i++)
            {
                var capturedField = capturedFields[i];
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, ctor.Parameters[i]);
                il.Emit(OpCodes.Stfld, fieldMap[capturedField]);
            }

            il.Emit(OpCodes.Ret);
            return ctor;
        }

        private static MethodDefinition CloneLambdaMethod(
            ModuleDefinition module,
            TypeDefinition adapterType,
            MethodDefinition lambda,
            DelegateSignature signature,
            TypeReference interfaceType,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap)
        {
            var interfaceMethod = interfaceType.Resolve().Methods.First(method =>
                !method.IsSpecialName &&
                method.HasThis);

            var method = new MethodDefinition(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
                CloseInterfaceType(module, interfaceMethod.ReturnType, interfaceType));

            foreach (var parameter in interfaceMethod.Parameters)
            {
                method.Parameters.Add(new ParameterDefinition(
                    parameter.Name,
                    parameter.Attributes,
                    CloseInterfaceType(module, parameter.ParameterType, interfaceType)));
            }

            method.Body.InitLocals = lambda.Body.InitLocals;

            var variableMap = new Dictionary<VariableDefinition, VariableDefinition>();
            foreach (var variable in lambda.Body.Variables)
            {
                var mappedVariable = new VariableDefinition(module.ImportReference(variable.VariableType));
                method.Body.Variables.Add(mappedVariable);
                variableMap.Add(variable, mappedVariable);
            }

            var il = method.Body.GetILProcessor();
            var instructionMap = new Dictionary<Instruction, Instruction>();

            foreach (var sourceInstruction in lambda.Body.Instructions)
            {
                var first = AppendClonedInstruction(
                    module,
                    il,
                    sourceInstruction,
                    signature,
                    fieldMap,
                    variableMap,
                    lambda,
                    method);
                instructionMap.Add(sourceInstruction, first);
            }

            foreach (var cloned in method.Body.Instructions)
            {
                if (cloned.Operand is Instruction target)
                {
                    cloned.Operand = instructionMap[target];
                }
                else if (cloned.Operand is Instruction[] targets)
                {
                    cloned.Operand = targets.Select(t => instructionMap[t]).ToArray();
                }
            }

            foreach (var exceptionHandler in lambda.Body.ExceptionHandlers)
            {
                method.Body.ExceptionHandlers.Add(new ExceptionHandler(exceptionHandler.HandlerType)
                {
                    CatchType = exceptionHandler.CatchType == null ? null : module.ImportReference(exceptionHandler.CatchType),
                    TryStart = instructionMap[exceptionHandler.TryStart],
                    TryEnd = instructionMap[exceptionHandler.TryEnd],
                    HandlerStart = instructionMap[exceptionHandler.HandlerStart],
                    HandlerEnd = instructionMap[exceptionHandler.HandlerEnd],
                    FilterStart = exceptionHandler.FilterStart == null ? null : instructionMap[exceptionHandler.FilterStart],
                });
            }

            return method;
        }

        private static Instruction AppendClonedInstruction(
            ModuleDefinition module,
            ILProcessor il,
            Instruction sourceInstruction,
            DelegateSignature signature,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap,
            IReadOnlyDictionary<VariableDefinition, VariableDefinition> variableMap,
            MethodDefinition sourceMethod,
            MethodDefinition targetMethod)
        {
            if (TryGetArgumentAccess(sourceInstruction, sourceMethod, out var argumentIndex, out var loadAddress, out var store))
            {
                var lambdaParameterOffset = sourceMethod.IsStatic ? 0 : 1;
                var lambdaParameterIndex = argumentIndex - lambdaParameterOffset;
                if (lambdaParameterIndex >= 0 && lambdaParameterIndex < signature.ParameterTypes.Count)
                {
                    if (store)
                    {
                        var starg = Instruction.Create(OpCodes.Starg, targetMethod.Parameters[lambdaParameterIndex]);
                        il.Append(starg);
                        return starg;
                    }

                    var load = CreateLoadArgument(targetMethod, lambdaParameterIndex + 1);
                    il.Append(load);
                    if (loadAddress || signature.ParameterTypes[lambdaParameterIndex] is ByReferenceType)
                    {
                        return load;
                    }

                    il.Append(Instruction.Create(OpCodes.Ldobj, module.ImportReference(signature.ParameterTypes[lambdaParameterIndex])));
                    return load;
                }
            }

            var cloned = Instruction.Create(OpCodes.Nop);
            cloned.OpCode = sourceInstruction.OpCode;
            cloned.Operand = CloneOperand(module, sourceInstruction.Operand, fieldMap, variableMap, sourceMethod, targetMethod);
            il.Append(cloned);
            return cloned;
        }

        private static bool TryGetArgumentAccess(
            Instruction instruction,
            MethodDefinition method,
            out int argumentIndex,
            out bool loadAddress,
            out bool store)
        {
            argumentIndex = -1;
            loadAddress = false;
            store = false;

            if (instruction.OpCode == OpCodes.Ldarg_0)
            {
                argumentIndex = 0;
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldarg_1)
            {
                argumentIndex = 1;
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldarg_2)
            {
                argumentIndex = 2;
                return true;
            }

            if (instruction.OpCode == OpCodes.Ldarg_3)
            {
                argumentIndex = 3;
                return true;
            }

            if (instruction.Operand is not ParameterDefinition parameter)
            {
                return false;
            }

            argumentIndex = method.HasThis ? parameter.Index + 1 : parameter.Index;
            loadAddress = instruction.OpCode == OpCodes.Ldarga || instruction.OpCode == OpCodes.Ldarga_S;
            store = instruction.OpCode == OpCodes.Starg || instruction.OpCode == OpCodes.Starg_S;
            return instruction.OpCode == OpCodes.Ldarg ||
                instruction.OpCode == OpCodes.Ldarg_S ||
                loadAddress ||
                store;
        }

        private static Instruction CreateLoadArgument(MethodDefinition method, int argumentIndex)
        {
            switch (argumentIndex)
            {
                case 0:
                    return Instruction.Create(OpCodes.Ldarg_0);
                case 1:
                    return Instruction.Create(OpCodes.Ldarg_1);
                case 2:
                    return Instruction.Create(OpCodes.Ldarg_2);
                case 3:
                    return Instruction.Create(OpCodes.Ldarg_3);
                default:
                    return Instruction.Create(OpCodes.Ldarg, method.Parameters[argumentIndex - 1]);
            }
        }

        private static TypeReference CloseInterfaceType(ModuleDefinition module, TypeReference type, TypeReference interfaceType)
        {
            var interfaceInstance = interfaceType as GenericInstanceType;
            switch (type)
            {
                case GenericParameter genericParameter when genericParameter.Type == GenericParameterType.Type && interfaceInstance != null:
                    return module.ImportReference(interfaceInstance.GenericArguments[genericParameter.Position]);
                case GenericInstanceType genericInstance:
                    var closedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        closedInstance.GenericArguments.Add(CloseInterfaceType(module, argument, interfaceType));
                    }

                    return closedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(CloseInterfaceType(module, byReference.ElementType, interfaceType));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        CloseInterfaceType(module, requiredModifier.ElementType, interfaceType));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        CloseInterfaceType(module, optionalModifier.ElementType, interfaceType));
                default:
                    return module.ImportReference(type);
            }
        }

        private static ParameterDefinition CreateReadonlyInParameter(ModuleDefinition module, TypeReference sourceType)
        {
            var inAttribute = new TypeReference(
                "System.Runtime.InteropServices",
                "InAttribute",
                module,
                module.TypeSystem.CoreLibrary);

            var isReadOnlyAttribute = new TypeReference(
                "System.Runtime.CompilerServices",
                "IsReadOnlyAttribute",
                module,
                module.TypeSystem.CoreLibrary);

            var parameter = new ParameterDefinition(
                "value",
                Mono.Cecil.ParameterAttributes.In,
                new RequiredModifierType(inAttribute, new ByReferenceType(module.ImportReference(sourceType))));

            var isReadOnlyCtor = new MethodReference(".ctor", module.TypeSystem.Void, isReadOnlyAttribute)
            {
                HasThis = true,
            };

            parameter.CustomAttributes.Add(new CustomAttribute(isReadOnlyCtor));
            return parameter;
        }

        private static object CloneOperand(
            ModuleDefinition module,
            object operand,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap,
            IReadOnlyDictionary<VariableDefinition, VariableDefinition> variableMap,
            MethodDefinition sourceMethod,
            MethodDefinition targetMethod)
        {
            switch (operand)
            {
                case null:
                    return null;
                case TypeReference type:
                    return module.ImportReference(type);
                case MethodReference method:
                    return module.ImportReference(method);
                case FieldReference field:
                    var resolvedField = field.Resolve();
                    return resolvedField != null && fieldMap.TryGetValue(resolvedField, out var adapterField)
                        ? adapterField
                        : module.ImportReference(field);
                case VariableDefinition variable:
                    return variableMap[variable];
                case ParameterDefinition parameter:
                    var sourceParameterIndex = sourceMethod.Parameters.IndexOf(parameter);
                    return sourceParameterIndex >= 0 && sourceParameterIndex < targetMethod.Parameters.Count
                        ? targetMethod.Parameters[sourceParameterIndex]
                        : parameter;
                default:
                    return operand;
            }
        }

        private static IReadOnlyList<FieldDefinition> GetCapturedFields(
            MethodDefinition lambda,
            List<DiagnosticMessage> diagnostics,
            MethodDefinition owner)
        {
            if (lambda.IsStatic)
            {
                return Array.Empty<FieldDefinition>();
            }

            var declaringType = lambda.DeclaringType;
            if (!declaringType.Name.StartsWith("<>c", StringComparison.Ordinal))
            {
                AddError(diagnostics, owner, $"NativeLinq delegate target '{lambda.FullName}' is not a compiler-generated lambda.");
                return null;
            }

            var fields = declaringType.Fields
                .Where(f => !f.IsStatic)
                .ToArray();

            foreach (var field in fields)
            {
                if (!IsUnmanaged(field.FieldType))
                {
                    AddError(diagnostics, owner, $"NativeLinq delegate capture '{field.Name}' has managed type '{field.FieldType.FullName}'.");
                    return null;
                }
            }

            return fields;
        }

        private static bool IsUnmanaged(TypeReference type)
        {
            type = type.GetElementType();
            if (type.IsPointer)
            {
                return true;
            }

            switch (type.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.Char:
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                case MetadataType.Int64:
                case MetadataType.UInt64:
                case MetadataType.Single:
                case MetadataType.Double:
                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
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

            return definition.Fields.Where(f => !f.IsStatic).All(f => IsUnmanaged(f.FieldType));
        }

        private static Instruction PreviousMeaningful(Instruction instruction)
        {
            var current = instruction.Previous;
            while (current != null && current.OpCode == OpCodes.Nop)
            {
                current = current.Previous;
            }

            return current;
        }

        private static Instruction NextMeaningful(Instruction instruction)
        {
            var current = instruction.Next;
            while (current != null && current.OpCode == OpCodes.Nop)
            {
                current = current.Next;
            }

            return current;
        }

        private static bool IsLoadLocal(MethodDefinition method, Instruction instruction, VariableDefinition expectedLocal)
        {
            return TryGetLoadedLocal(method, instruction, out var local) && local == expectedLocal;
        }

        private static bool IsStoreLocal(MethodDefinition method, Instruction instruction, VariableDefinition expectedLocal)
        {
            return TryGetStoredLocal(method, instruction, out var local) && local == expectedLocal;
        }

        private static bool TryGetLoadedLocal(MethodDefinition method, Instruction instruction, out VariableDefinition local)
        {
            local = null;
            if (instruction.OpCode == OpCodes.Ldloc || instruction.OpCode == OpCodes.Ldloc_S)
            {
                local = instruction.Operand as VariableDefinition;
                return local != null;
            }

            if (instruction.OpCode == OpCodes.Ldloc_0)
            {
                return TryGetLocal(method, 0, out local);
            }

            if (instruction.OpCode == OpCodes.Ldloc_1)
            {
                return TryGetLocal(method, 1, out local);
            }

            if (instruction.OpCode == OpCodes.Ldloc_2)
            {
                return TryGetLocal(method, 2, out local);
            }

            if (instruction.OpCode == OpCodes.Ldloc_3)
            {
                return TryGetLocal(method, 3, out local);
            }

            return false;
        }

        private static bool TryGetStoredLocal(MethodDefinition method, Instruction instruction, out VariableDefinition local)
        {
            local = null;
            if (instruction.OpCode == OpCodes.Stloc || instruction.OpCode == OpCodes.Stloc_S)
            {
                local = instruction.Operand as VariableDefinition;
                return local != null;
            }

            if (instruction.OpCode == OpCodes.Stloc_0)
            {
                return TryGetLocal(method, 0, out local);
            }

            if (instruction.OpCode == OpCodes.Stloc_1)
            {
                return TryGetLocal(method, 1, out local);
            }

            if (instruction.OpCode == OpCodes.Stloc_2)
            {
                return TryGetLocal(method, 2, out local);
            }

            if (instruction.OpCode == OpCodes.Stloc_3)
            {
                return TryGetLocal(method, 3, out local);
            }

            return false;
        }

        private static bool TryGetLocal(MethodDefinition method, int index, out VariableDefinition local)
        {
            local = null;
            if (index < 0 || index >= method.Body.Variables.Count)
            {
                return false;
            }

            local = method.Body.Variables[index];
            return true;
        }

        private static Instruction FindStackProducerStart(Instruction end, int requiredValues)
        {
            var needed = requiredValues;
            var current = end;
            while (current != null)
            {
                if (!TryGetStackDelta(current, out var pushes, out var pops))
                {
                    return null;
                }

                needed = needed - pushes + pops;
                if (needed <= 0)
                {
                    return current;
                }

                current = PreviousMeaningful(current);
            }

            return null;
        }

        private static bool TryGetStackDelta(Instruction instruction, out int pushes, out int pops)
        {
            pushes = GetPushCount(instruction);
            return TryGetPopCount(instruction, out pops);
        }

        private static int GetPushCount(Instruction instruction)
        {
            switch (instruction.OpCode.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                    return 0;
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    return 1;
                case StackBehaviour.Push1_push1:
                    return 2;
                case StackBehaviour.Varpush:
                    return instruction.Operand is MethodReference method &&
                        method.ReturnType.MetadataType != MetadataType.Void
                        ? 1
                        : 0;
                default:
                    return 0;
            }
        }

        private static bool TryGetPopCount(Instruction instruction, out int pops)
        {
            switch (instruction.OpCode.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    pops = 0;
                    return true;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    pops = 1;
                    return true;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    pops = 2;
                    return true;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    pops = 3;
                    return true;
                case StackBehaviour.Varpop:
                    if (instruction.Operand is MethodReference method)
                    {
                        pops = method.Parameters.Count;
                        if (method.HasThis && instruction.OpCode != OpCodes.Newobj)
                        {
                            pops++;
                        }

                        return true;
                    }

                    pops = 0;
                    return false;
                default:
                    pops = 0;
                    return false;
            }
        }

        private static bool TryGetCaptureLocal(
            object operand,
            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals,
            out FieldDefinition capturedField,
            out VariableDefinition local)
        {
            capturedField = null;
            local = null;
            if (operand is not FieldReference fieldReference)
            {
                return false;
            }

            var resolvedField = fieldReference.Resolve();
            foreach (var pair in captureLocals)
            {
                if (pair.Key == resolvedField || pair.Key.FullName == resolvedField?.FullName)
                {
                    capturedField = pair.Key;
                    local = pair.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool SameType(TypeReference left, TypeReference right)
        {
            return left != null && right != null && left.FullName == right.FullName;
        }

        private static void MakeNop(Instruction instruction)
        {
            instruction.OpCode = OpCodes.Nop;
            instruction.Operand = null;
        }

        private static void AddError(List<DiagnosticMessage> diagnostics, MethodDefinition method, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                MessageData = $"{message} Method: {method.FullName}",
            });
        }

        private static AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
        {
            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = resolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate,
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
            resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);
            return assemblyDefinition;
        }

        private sealed class AdapterInfo
        {
            public AdapterInfo(TypeReference adapterType, TypeReference interfaceType)
            {
                AdapterType = adapterType;
                InterfaceType = interfaceType;
            }

            public TypeReference AdapterType { get; }

            public TypeReference InterfaceType { get; }
        }

        private sealed class DelegateSignature
        {
            public DelegateSignature(IReadOnlyList<TypeReference> parameterTypes, TypeReference returnType)
            {
                ParameterTypes = parameterTypes;
                ReturnType = returnType;
            }

            public IReadOnlyList<TypeReference> ParameterTypes { get; }

            public TypeReference ReturnType { get; }
        }

        private sealed class TargetMethodInfo
        {
            public TargetMethodInfo(MethodReference call, TypeReference returnType)
            {
                Call = call;
                ReturnType = returnType;
            }

            public MethodReference Call { get; }

            public TypeReference ReturnType { get; }
        }

        private sealed class PostProcessorAssemblyResolver : IAssemblyResolver
        {
            private readonly ICompiledAssembly compiledAssembly;
            private readonly Dictionary<string, HashSet<string>> referenceToPathMap = new Dictionary<string, HashSet<string>>();
            private readonly Dictionary<string, AssemblyDefinition> cache = new Dictionary<string, AssemblyDefinition>();
            private readonly string[] referenceDirectories;
            private AssemblyDefinition selfAssembly;

            public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
            {
                this.compiledAssembly = compiledAssembly;

                foreach (var reference in compiledAssembly.References)
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(reference);
                    if (!referenceToPathMap.TryGetValue(assemblyName, out var paths))
                    {
                        paths = new HashSet<string>();
                        referenceToPathMap.Add(assemblyName, paths);
                    }

                    paths.Add(reference);
                }

                referenceDirectories = referenceToPathMap.Values.SelectMany(p => p.Select(Path.GetDirectoryName)).Distinct().ToArray();
            }

            public void Dispose()
            {
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                if (name.Name == compiledAssembly.Name)
                {
                    return selfAssembly;
                }

                var fileName = FindFile(name);
                if (fileName == null)
                {
                    return null;
                }

                if (cache.TryGetValue(fileName, out var assembly))
                {
                    return assembly;
                }

                parameters.AssemblyResolver = this;
                var stream = MemoryStreamFor(fileName);
                var pdb = fileName + ".pdb";
                if (File.Exists(pdb))
                {
                    parameters.SymbolStream = MemoryStreamFor(pdb);
                }

                assembly = AssemblyDefinition.ReadAssembly(stream, parameters);
                cache.Add(fileName, assembly);
                return assembly;
            }

            public void AddAssemblyDefinitionBeingOperatedOn(AssemblyDefinition assemblyDefinition)
            {
                selfAssembly = assemblyDefinition;
            }

            private string FindFile(AssemblyNameReference name)
            {
                if (referenceToPathMap.TryGetValue(name.Name, out var paths))
                {
                    if (paths.Count == 1)
                    {
                        return paths.First();
                    }

                    foreach (var path in paths)
                    {
                        if (System.Reflection.AssemblyName.GetAssemblyName(path).FullName == name.FullName)
                        {
                            return path;
                        }
                    }
                }

                foreach (var directory in referenceDirectories)
                {
                    var candidate = Path.Combine(directory, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                return null;
            }

            private static MemoryStream MemoryStreamFor(string fileName)
            {
                return new MemoryStream(File.ReadAllBytes(fileName));
            }
        }

        private sealed class PostProcessorReflectionImporterProvider : IReflectionImporterProvider
        {
            public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
            {
                return new PostProcessorReflectionImporter(module);
            }
        }

        private sealed class PostProcessorReflectionImporter : DefaultReflectionImporter
        {
            public PostProcessorReflectionImporter(ModuleDefinition module)
                : base(module)
            {
            }

            public override AssemblyNameReference ImportReference(System.Reflection.AssemblyName reference)
            {
                var importedReference = base.ImportReference(reference);
                if (importedReference.Name == "System.Private.CoreLib")
                {
                    importedReference.Name = "mscorlib";
                }

                return importedReference;
            }
        }
    }
}
