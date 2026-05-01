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
        private const string DelegateExtensionsTypeName = "KrasCore.NativeLinqDelegateExtensions";
        private const string QueryTypeName = "KrasCore.Query`2";
        private const string WhereQueryTypeName = "KrasCore.WhereQuery`3";
        private const string SelectQueryTypeName = "KrasCore.SelectQuery`4";
        private const string PredicateInterfaceTypeName = "KrasCore.IPredicate`1";
        private const string SelectorInterfaceTypeName = "KrasCore.ISelector`2";

        private int adapterIndex;
        private readonly Dictionary<string, TypeReference> rewrittenEnumeratorTypes = new Dictionary<string, TypeReference>();

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

        private bool ProcessMethod(MethodDefinition method, List<DiagnosticMessage> diagnostics)
        {
            var modified = false;
            var instructions = method.Body.Instructions;

            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if (instruction.OpCode != OpCodes.Call || instruction.Operand is not MethodReference call ||
                    call.DeclaringType.FullName != DelegateExtensionsTypeName)
                {
                    continue;
                }

                if (call.Name == "WithDelegates")
                {
                    instruction.OpCode = OpCodes.Nop;
                    instruction.Operand = null;
                    modified = true;
                    continue;
                }

                if (call is not GenericInstanceMethod genericCall)
                {
                    continue;
                }

                if (call.Name == "Where")
                {
                    modified |= RewriteWhere(method, instruction, genericCall, diagnostics);
                }
                else if (call.Name == "Select")
                {
                    modified |= RewriteSelect(method, instruction, genericCall, diagnostics);
                }
                else if (call.Name == "Sum")
                {
                    modified |= RewriteSum(method, instruction, genericCall, diagnostics);
                }
            }

            if (modified)
            {
                method.Body.OptimizeMacros();
            }

            return modified;
        }

        private bool RewriteWhere(
            MethodDefinition owner,
            Instruction callInstruction,
            GenericInstanceMethod placeholderCall,
            List<DiagnosticMessage> diagnostics)
        {
            var module = owner.Module;
            var sourceType = module.ImportReference(placeholderCall.GenericArguments[0]);
            var enumeratorType = ResolveRewrittenEnumerator(module, placeholderCall.GenericArguments[1]);
            var adapter = CreateAdapter(owner, callInstruction, sourceType, module.TypeSystem.Boolean, true, diagnostics);
            if (adapter == null)
            {
                return false;
            }

            var realWhereQueryType = CreateWhereQueryType(module, sourceType, enumeratorType, adapter.AdapterType);
            MapRewrittenEnumerator(CloseMethodGenericType(module, placeholderCall.ReturnType, placeholderCall), realWhereQueryType);
            PrepareValueTypeInstanceCall(
                owner,
                callInstruction,
                CreateQueryType(module, sourceType, enumeratorType),
                new[] { adapter.AdapterType });
            callInstruction.Operand = CreateQueryMethodCall(
                module,
                "Where",
                sourceType,
                enumeratorType,
                new[] { adapter.AdapterType });

            return true;
        }

        private bool RewriteSelect(
            MethodDefinition owner,
            Instruction callInstruction,
            GenericInstanceMethod placeholderCall,
            List<DiagnosticMessage> diagnostics)
        {
            var module = owner.Module;
            var sourceType = module.ImportReference(placeholderCall.GenericArguments[0]);
            var resultType = module.ImportReference(placeholderCall.GenericArguments[1]);
            var enumeratorType = ResolveRewrittenEnumerator(module, placeholderCall.GenericArguments[2]);
            var adapter = CreateAdapter(owner, callInstruction, sourceType, resultType, false, diagnostics);
            if (adapter == null)
            {
                return false;
            }

            var realSelectQueryType = CreateSelectQueryType(module, sourceType, resultType, enumeratorType, adapter.AdapterType);
            MapRewrittenEnumerator(CloseMethodGenericType(module, placeholderCall.ReturnType, placeholderCall), realSelectQueryType);
            PrepareValueTypeInstanceCall(
                owner,
                callInstruction,
                CreateQueryType(module, sourceType, enumeratorType),
                new[] { adapter.AdapterType });
            callInstruction.Operand = CreateQueryMethodCall(
                module,
                "Select",
                sourceType,
                enumeratorType,
                new[] { resultType, adapter.AdapterType });

            return true;
        }

        private bool RewriteSum(
            MethodDefinition owner,
            Instruction callInstruction,
            GenericInstanceMethod placeholderCall,
            List<DiagnosticMessage> diagnostics)
        {
            var module = owner.Module;
            var sourceType = module.ImportReference(placeholderCall.GenericArguments[0]);
            var resultType = module.ImportReference(placeholderCall.GenericArguments[1]);
            var enumeratorType = ResolveRewrittenEnumerator(module, placeholderCall.GenericArguments[2]);
            var adapter = CreateAdapter(owner, callInstruction, sourceType, resultType, false, diagnostics);
            if (adapter == null)
            {
                return false;
            }

            var accumulatorType = ResolveAccumulatorType(module, resultType);
            if (accumulatorType == null)
            {
                AddError(diagnostics, owner, $"NativeLinq delegate Sum does not have an accumulator for '{resultType.FullName}'.");
                return false;
            }

            var accumulatorLocal = new VariableDefinition(accumulatorType);
            owner.Body.Variables.Add(accumulatorLocal);
            owner.Body.InitLocals = true;

            var il = owner.Body.GetILProcessor();
            il.InsertBefore(callInstruction, il.Create(OpCodes.Ldloca, accumulatorLocal));
            il.InsertBefore(callInstruction, il.Create(OpCodes.Initobj, accumulatorType));
            il.InsertBefore(callInstruction, il.Create(OpCodes.Ldloc, accumulatorLocal));

            PrepareValueTypeInstanceCall(
                owner,
                callInstruction,
                CreateQueryType(module, sourceType, enumeratorType),
                new[] { adapter.AdapterType, accumulatorType });
            callInstruction.Operand = CreateQueryMethodCall(
                module,
                "Sum",
                sourceType,
                enumeratorType,
                new[] { resultType, adapter.AdapterType, accumulatorType });

            return true;
        }

        private static void PrepareValueTypeInstanceCall(
            MethodDefinition owner,
            Instruction callInstruction,
            TypeReference receiverType,
            IReadOnlyList<TypeReference> argumentTypes)
        {
            var il = owner.Body.GetILProcessor();
            var argumentLocals = new VariableDefinition[argumentTypes.Count];
            for (var i = argumentTypes.Count - 1; i >= 0; i--)
            {
                var local = new VariableDefinition(owner.Module.ImportReference(argumentTypes[i]));
                owner.Body.Variables.Add(local);
                argumentLocals[i] = local;
                il.InsertBefore(callInstruction, il.Create(OpCodes.Stloc, local));
            }

            var receiverLocal = new VariableDefinition(owner.Module.ImportReference(receiverType));
            owner.Body.Variables.Add(receiverLocal);
            owner.Body.InitLocals = true;

            il.InsertBefore(callInstruction, il.Create(OpCodes.Stloc, receiverLocal));
            il.InsertBefore(callInstruction, il.Create(OpCodes.Ldloca, receiverLocal));

            foreach (var local in argumentLocals)
            {
                il.InsertBefore(callInstruction, il.Create(OpCodes.Ldloc, local));
            }
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

        private void MapRewrittenEnumerator(TypeReference placeholderReturnType, TypeReference realEnumeratorType)
        {
            if (placeholderReturnType is not GenericInstanceType queryType || queryType.GenericArguments.Count != 2)
            {
                return;
            }

            rewrittenEnumeratorTypes[queryType.GenericArguments[1].FullName] = realEnumeratorType;
        }

        private TypeReference ResolveRewrittenEnumerator(ModuleDefinition module, TypeReference enumeratorType)
        {
            return rewrittenEnumeratorTypes.TryGetValue(enumeratorType.FullName, out var rewritten)
                ? module.ImportReference(rewritten)
                : module.ImportReference(enumeratorType);
        }

        private AdapterInfo CreateAdapter(
            MethodDefinition owner,
            Instruction callInstruction,
            TypeReference sourceType,
            TypeReference resultType,
            bool predicate,
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
            var targetType = lambda.IsStatic ? null : lambda.DeclaringType;
            var capturedFields = GetCapturedFields(lambda, diagnostics, owner);
            if (capturedFields == null)
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
            var interfaceType = predicate
                ? CreatePredicateInterfaceType(module, sourceType)
                : CreateSelectorInterfaceType(module, sourceType, resultType);

            adapterType.Interfaces.Add(new InterfaceImplementation(interfaceType));

            var fieldMap = new Dictionary<FieldDefinition, FieldDefinition>();
            foreach (var capturedField in capturedFields)
            {
                var adapterField = new FieldDefinition(capturedField.Name, FieldAttributes.Private, module.ImportReference(capturedField.FieldType));
                adapterType.Fields.Add(adapterField);
                fieldMap.Add(capturedField, adapterField);
            }

            var ctor = CreateAdapterConstructor(module, adapterType, targetType, fieldMap);
            adapterType.Methods.Add(ctor);
            adapterType.Methods.Add(CloneLambdaMethod(module, adapterType, lambda, sourceType, resultType, predicate, fieldMap));

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

            return new AdapterInfo(adapterTypeReference);
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
            instructionsToRemove.Add(duplicateForStore);
            instructionsToRemove.Add(storeCache);
            return delegateCtor;
        }

        private static MethodDefinition CreateAdapterConstructor(
            ModuleDefinition module,
            TypeDefinition adapterType,
            TypeReference targetType,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap)
        {
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            if (fieldMap.Count != 0)
            {
                ctor.Parameters.Add(new ParameterDefinition("target", Mono.Cecil.ParameterAttributes.None, module.ImportReference(targetType)));
            }

            var il = ctor.Body.GetILProcessor();
            foreach (var pair in fieldMap)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldfld, module.ImportReference(pair.Key));
                il.Emit(OpCodes.Stfld, pair.Value);
            }

            il.Emit(OpCodes.Ret);
            return ctor;
        }

        private static MethodDefinition CloneLambdaMethod(
            ModuleDefinition module,
            TypeDefinition adapterType,
            MethodDefinition lambda,
            TypeReference sourceType,
            TypeReference resultType,
            bool predicate,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap)
        {
            var method = new MethodDefinition(
                predicate ? "Match" : "Select",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
                module.ImportReference(resultType));

            method.Parameters.Add(CreateReadonlyInParameter(module, sourceType));
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
                var cloned = Instruction.Create(OpCodes.Nop);
                cloned.OpCode = RemapArgumentOpcode(sourceInstruction.OpCode, lambda.IsStatic);
                cloned.Operand = CloneOperand(module, sourceInstruction.Operand, fieldMap, variableMap, lambda, method);
                instructionMap.Add(sourceInstruction, cloned);
                il.Append(cloned);
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

        private static OpCode RemapArgumentOpcode(OpCode opcode, bool sourceMethodIsStatic)
        {
            return sourceMethodIsStatic && opcode == OpCodes.Ldarg_0 ? OpCodes.Ldarg_1 : opcode;
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
                    return sourceMethod.Parameters.IndexOf(parameter) == 0 ? targetMethod.Parameters[0] : parameter;
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

        private static TypeReference CreateQueryType(ModuleDefinition module, TypeReference valueType, TypeReference enumeratorType)
        {
            var query = new GenericInstanceType(CreateKrasCoreType(module, QueryTypeName, true));
            query.GenericArguments.Add(module.ImportReference(valueType));
            query.GenericArguments.Add(module.ImportReference(enumeratorType));
            return query;
        }

        private static TypeReference CreateWhereQueryType(ModuleDefinition module, TypeReference sourceType, TypeReference enumeratorType, TypeReference predicateType)
        {
            var query = new GenericInstanceType(CreateKrasCoreType(module, WhereQueryTypeName, true));
            query.GenericArguments.Add(module.ImportReference(sourceType));
            query.GenericArguments.Add(module.ImportReference(enumeratorType));
            query.GenericArguments.Add(module.ImportReference(predicateType));
            return query;
        }

        private static TypeReference CreateSelectQueryType(ModuleDefinition module, TypeReference sourceType, TypeReference resultType, TypeReference enumeratorType, TypeReference selectorType)
        {
            var query = new GenericInstanceType(CreateKrasCoreType(module, SelectQueryTypeName, true));
            query.GenericArguments.Add(module.ImportReference(sourceType));
            query.GenericArguments.Add(module.ImportReference(resultType));
            query.GenericArguments.Add(module.ImportReference(enumeratorType));
            query.GenericArguments.Add(module.ImportReference(selectorType));
            return query;
        }

        private static TypeReference CreatePredicateInterfaceType(ModuleDefinition module, TypeReference sourceType)
        {
            var interfaceType = new GenericInstanceType(CreateKrasCoreType(module, PredicateInterfaceTypeName, false));
            interfaceType.GenericArguments.Add(module.ImportReference(sourceType));
            return interfaceType;
        }

        private static TypeReference CreateSelectorInterfaceType(ModuleDefinition module, TypeReference sourceType, TypeReference resultType)
        {
            var interfaceType = new GenericInstanceType(CreateKrasCoreType(module, SelectorInterfaceTypeName, false));
            interfaceType.GenericArguments.Add(module.ImportReference(sourceType));
            interfaceType.GenericArguments.Add(module.ImportReference(resultType));
            return interfaceType;
        }

        private static MethodReference CreateQueryMethodCall(
            ModuleDefinition module,
            string methodName,
            TypeReference sourceType,
            TypeReference enumeratorType,
            IReadOnlyList<TypeReference> methodGenericArguments)
        {
            var queryType = (GenericInstanceType)CreateQueryType(module, sourceType, enumeratorType);
            var queryDefinition = queryType.Resolve();
            var methodDefinition = queryDefinition.Methods.First(m =>
                m.Name == methodName &&
                m.HasGenericParameters &&
                m.GenericParameters.Count == methodGenericArguments.Count);

            var methodReference = new MethodReference(methodDefinition.Name, module.TypeSystem.Void, queryType)
            {
                HasThis = methodDefinition.HasThis,
                ExplicitThis = methodDefinition.ExplicitThis,
                CallingConvention = methodDefinition.CallingConvention,
            };

            foreach (var genericParameter in methodDefinition.GenericParameters)
            {
                methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
            }

            methodReference.ReturnType = ImportOpenMethodSignatureType(
                module,
                methodDefinition.ReturnType,
                queryType.ElementType,
                methodReference);

            foreach (var parameter in methodDefinition.Parameters)
            {
                methodReference.Parameters.Add(new ParameterDefinition(
                    ImportOpenMethodSignatureType(module, parameter.ParameterType, queryType.ElementType, methodReference)));
            }

            var genericMethod = new GenericInstanceMethod(methodReference);
            foreach (var argument in methodGenericArguments)
            {
                genericMethod.GenericArguments.Add(module.ImportReference(argument));
            }

            return genericMethod;
        }

        private static TypeReference ImportOpenMethodSignatureType(
            ModuleDefinition module,
            TypeReference type,
            TypeReference typeGenericOwner,
            MethodReference methodGenericOwner)
        {
            switch (type)
            {
                case GenericParameter genericParameter:
                    return genericParameter.Type == GenericParameterType.Method
                        ? methodGenericOwner.GenericParameters[genericParameter.Position]
                        : typeGenericOwner.GenericParameters[genericParameter.Position];
                case GenericInstanceType genericInstance:
                    var importedInstance = new GenericInstanceType(module.ImportReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        importedInstance.GenericArguments.Add(ImportOpenMethodSignatureType(
                            module,
                            argument,
                            typeGenericOwner,
                            methodGenericOwner));
                    }

                    return importedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(ImportOpenMethodSignatureType(
                        module,
                        byReference.ElementType,
                        typeGenericOwner,
                        methodGenericOwner));
                case PointerType pointer:
                    return new PointerType(ImportOpenMethodSignatureType(
                        module,
                        pointer.ElementType,
                        typeGenericOwner,
                        methodGenericOwner));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        module.ImportReference(requiredModifier.ModifierType),
                        ImportOpenMethodSignatureType(module, requiredModifier.ElementType, typeGenericOwner, methodGenericOwner));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        module.ImportReference(optionalModifier.ModifierType),
                        ImportOpenMethodSignatureType(module, optionalModifier.ElementType, typeGenericOwner, methodGenericOwner));
                default:
                    return module.ImportReference(type);
            }
        }

        private static TypeReference ResolveAccumulatorType(ModuleDefinition module, TypeReference resultType)
        {
            var name = resultType.MetadataType switch
            {
                MetadataType.SByte => "SByteAccumulator",
                MetadataType.Byte => "ByteAccumulator",
                MetadataType.Int16 => "Int16Accumulator",
                MetadataType.UInt16 => "UInt16Accumulator",
                MetadataType.Int32 => "Int32Accumulator",
                MetadataType.UInt32 => "UInt32Accumulator",
                MetadataType.Int64 => "Int64Accumulator",
                MetadataType.UInt64 => "UInt64Accumulator",
                MetadataType.Single => "SingleAccumulator",
                MetadataType.Double => "DoubleAccumulator",
                _ => $"{resultType.Name}Accumulator",
            };

            var accumulatorType = CreateKrasCoreType(module, $"KrasCore.{name}", true);
            return accumulatorType.Resolve() == null ? null : accumulatorType;
        }

        private static TypeReference CreateKrasCoreType(ModuleDefinition module, string fullName, bool isValueType)
        {
            var index = fullName.LastIndexOf('.');
            var @namespace = fullName.Substring(0, index);
            var name = fullName.Substring(index + 1);
            var scope = module.Assembly.Name.Name == KrasCoreAssemblyName
                ? module.Assembly.Name
                : module.AssemblyReferences.First(r => r.Name == KrasCoreAssemblyName);

            var type = new TypeReference(@namespace, name, module, scope, isValueType);
            var arityMarker = name.LastIndexOf('`');
            if (arityMarker >= 0 &&
                int.TryParse(name.Substring(arityMarker + 1), out var arity))
            {
                for (var i = 0; i < arity; i++)
                {
                    type.GenericParameters.Add(new GenericParameter($"T{i}", type));
                }
            }

            return type;
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
            public AdapterInfo(TypeReference adapterType)
            {
                AdapterType = adapterType;
            }

            public TypeReference AdapterType { get; }
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
