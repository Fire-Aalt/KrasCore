using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor
    {
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
    }
}
