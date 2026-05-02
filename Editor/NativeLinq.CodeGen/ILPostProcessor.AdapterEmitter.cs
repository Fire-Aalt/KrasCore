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
                    AddError(diagnostics, owner, callInstruction, "NativeLinq delegate weaving only supports direct lambda or compiler-cached static lambda delegate construction.");
                    return null;
                }
            }

            var functionInstruction = PreviousMeaningful(delegateCtorInstruction);
            if (functionInstruction?.OpCode != OpCodes.Ldftn ||
                functionInstruction.Operand is not MethodReference lambdaReference)
            {
                AddError(diagnostics, owner, callInstruction, "NativeLinq delegate weaving could not find the lambda method.");
                return null;
            }

            var lambda = lambdaReference.Resolve();
            if (lambda == null)
            {
                AddError(diagnostics, owner, callInstruction, $"NativeLinq delegate weaving could not resolve '{lambdaReference.FullName}'.");
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

            var capturesInstanceTarget = !lambda.IsStatic && !IsCompilerGeneratedLambdaContainer(lambda.DeclaringType);
            if (capturesInstanceTarget)
            {
                if (targetInstruction == null)
                {
                    AddError(diagnostics, owner, callInstruction, "NativeLinq delegate weaving could not find the instance method target.");
                    return null;
                }

                if (!IsUnmanaged(lambdaReference.DeclaringType))
                {
                    AddError(diagnostics, owner, callInstruction, $"NativeLinq delegate target '{lambdaReference.DeclaringType.FullName}' has managed type.");
                    return null;
                }
            }

            var capturedFields = GetCapturedFields(lambda, diagnostics, owner, callInstruction);
            if (capturedFields == null)
            {
                return null;
            }

            if (!ValidateLambdaBodyUsesOnlyUnmanagedTypes(lambda, capturedFields, diagnostics, owner, callInstruction))
            {
                return null;
            }

            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals = null;
            if (capturedFields.Count != 0 &&
                !TryRewriteClosureCaptures(owner, lambda.DeclaringType, targetInstruction, capturedFields, diagnostics, callInstruction, out captureLocals))
            {
                return null;
            }

            var module = owner.Module;
            var adapterType = new TypeDefinition(
                "KrasCore.Generated.NativeLinq",
                $"__NativeLinqDelegateAdapter_{_adapterIndex++}",
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
                module.ImportReference(typeof(ValueType)));

            owner.DeclaringType.NestedTypes.Add(adapterType);

            var adapterTypeReference = module.ImportReference(adapterType);
            adapterType.Interfaces.Add(new InterfaceImplementation(interfaceType));

            var fieldMap = new Dictionary<FieldDefinition, FieldDefinition>();
            FieldDefinition instanceTargetField = null;
            if (capturesInstanceTarget)
            {
                instanceTargetField = new FieldDefinition("__target", FieldAttributes.Private, module.ImportReference(lambdaReference.DeclaringType));
                adapterType.Fields.Add(instanceTargetField);
            }

            foreach (var capturedField in capturedFields)
            {
                var adapterField = new FieldDefinition(capturedField.Name, FieldAttributes.Private, module.ImportReference(capturedField.FieldType));
                adapterType.Fields.Add(adapterField);
                fieldMap.Add(capturedField, adapterField);
            }

            if (!capturesInstanceTarget && capturedFields.Count == 0)
            {
                adapterType.PackingSize = 0;
                adapterType.ClassSize = 1;
            }

            var ctor = CreateAdapterConstructor(module, adapterType, instanceTargetField, capturedFields, fieldMap);
            adapterType.Methods.Add(ctor);
            adapterType.Methods.Add(CloneLambdaMethod(module, adapterType, lambda, signature, interfaceType, instanceTargetField, fieldMap));

            foreach (var instruction in instructionsToRemove)
            {
                instruction.OpCode = OpCodes.Nop;
                instruction.Operand = null;
            }

            functionInstruction.OpCode = OpCodes.Nop;
            functionInstruction.Operand = null;
            delegateCtorInstruction.Operand = ctor;

            if (capturesInstanceTarget)
            {
                if (targetInstruction.OpCode == OpCodes.Box &&
                    targetInstruction.Operand is TypeReference boxedType &&
                    TypeDefinitionsMatch(boxedType, lambdaReference.DeclaringType))
                {
                    targetInstruction.OpCode = OpCodes.Nop;
                    targetInstruction.Operand = null;
                }
            }
            else if (capturedFields.Count == 0 && targetInstruction != null)
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
            Instruction diagnosticInstruction,
            out IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals)
        {
            captureLocals = null;
            if (targetInstruction == null ||
                !TryGetLoadedLocal(owner, targetInstruction, out var closureLocal))
            {
                AddError(diagnostics, owner, diagnosticInstruction, "NativeLinq delegate weaving only supports local variable captures.");
                return false;
            }

            var key = $"{owner.FullName}|{owner.Body.Variables.IndexOf(closureLocal)}|{closureType.FullName}";
            if (_rewrittenCaptureLocals.TryGetValue(key, out captureLocals))
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
            if (!RewriteClosureLocalAccesses(owner, closureType, closureLocal, locals, diagnostics, diagnosticInstruction))
            {
                return false;
            }

            _rewrittenCaptureLocals.Add(key, locals);
            captureLocals = locals;
            return true;
        }

        private static bool RewriteClosureLocalAccesses(
            MethodDefinition owner,
            TypeDefinition closureType,
            VariableDefinition closureLocal,
            IReadOnlyDictionary<FieldDefinition, VariableDefinition> captureLocals,
            List<DiagnosticMessage> diagnostics,
            Instruction diagnosticInstruction)
        {
            foreach (var instruction in owner.Body.Instructions.ToArray())
            {
                if (instruction.OpCode == OpCodes.Newobj &&
                    instruction.Operand is MethodReference { Name: ".ctor" } ctor &&
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
                        AddError(diagnostics, owner, diagnosticInstruction, "NativeLinq delegate weaving only supports direct captured local assignments.");
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
                        AddError(diagnostics, owner, diagnosticInstruction, "NativeLinq delegate weaving only supports direct captured local reads.");
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
            FieldDefinition instanceTargetField,
            IReadOnlyList<FieldDefinition> capturedFields,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap)
        {
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            if (instanceTargetField != null)
            {
                ctor.Parameters.Add(new ParameterDefinition(
                    "target",
                    Mono.Cecil.ParameterAttributes.None,
                    module.ImportReference(instanceTargetField.FieldType)));
            }

            foreach (var capturedField in capturedFields)
            {
                ctor.Parameters.Add(new ParameterDefinition(
                    capturedField.Name,
                    Mono.Cecil.ParameterAttributes.None,
                    module.ImportReference(capturedField.FieldType)));
            }

            var il = ctor.Body.GetILProcessor();
            if (instanceTargetField != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, ctor.Parameters[0]);
                il.Emit(OpCodes.Stfld, instanceTargetField);
            }

            for (var i = 0; i < capturedFields.Count; i++)
            {
                var capturedField = capturedFields[i];
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, ctor.Parameters[i + (instanceTargetField == null ? 0 : 1)]);
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
            FieldDefinition instanceTargetField,
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
                    instanceTargetField,
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
            FieldDefinition instanceTargetField,
            IReadOnlyDictionary<FieldDefinition, FieldDefinition> fieldMap,
            IReadOnlyDictionary<VariableDefinition, VariableDefinition> variableMap,
            MethodDefinition sourceMethod,
            MethodDefinition targetMethod)
        {
            if (TryGetArgumentAccess(sourceInstruction, sourceMethod, out var argumentIndex, out var loadAddress, out var store))
            {
                if (sourceMethod.HasThis && argumentIndex == 0 && instanceTargetField != null)
                {
                    var loadThis = Instruction.Create(OpCodes.Ldarg_0);
                    il.Append(loadThis);
                    il.Append(Instruction.Create(OpCodes.Ldflda, instanceTargetField));
                    return loadThis;
                }

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
            return RewriteTypeReference(
                type,
                genericParameter => genericParameter.Type == GenericParameterType.Type && interfaceInstance != null
                    ? module.ImportReference(interfaceInstance.GenericArguments[genericParameter.Position])
                    : null,
                module.ImportReference);
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
    }
}
