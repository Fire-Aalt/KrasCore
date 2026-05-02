using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor : Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor
    {
        private const string KrasCoreAssemblyName = "KrasCore";
        private const string NativeDelegateMethodAttributeTypeName = "KrasCore.NativeDelegateMethodAttribute";

        private int _adapterIndex;
        private readonly Dictionary<string, TypeReference> _rewrittenEnumeratorTypes = new();
        private readonly Dictionary<string, IReadOnlyDictionary<FieldDefinition, VariableDefinition>> _rewrittenCaptureLocals = new();

        public override Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
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
            _rewrittenEnumeratorTypes.Clear();
            _rewrittenCaptureLocals.Clear();
            var instructions = method.Body.Instructions;

            foreach (var instruction in instructions)
            {
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
    }
}
