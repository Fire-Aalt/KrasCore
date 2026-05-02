using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor
    {
        private static void AddError(
            List<DiagnosticMessage> diagnostics,
            MethodDefinition method,
            Instruction instruction,
            string message)
        {
            AddError(diagnostics, method, instruction, null, null, message);
        }

        private static void AddError(
            List<DiagnosticMessage> diagnostics,
            MethodDefinition method,
            Instruction instruction,
            MethodDefinition fallbackMethod,
            Instruction fallbackInstruction,
            string message)
        {
            var diagnostic = new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                MessageData = $"{message} Method: {method.FullName}",
            };

            var sequencePoint = FindBestSequencePointFor(method, instruction) ??
                FindBestSequencePointFor(fallbackMethod, fallbackInstruction);
            if (sequencePoint != null)
            {
                diagnostic.File = sequencePoint.Document.Url;
                diagnostic.Line = sequencePoint.StartLine;
                diagnostic.Column = sequencePoint.StartColumn;

                var shortenedFilePath = sequencePoint.Document.Url.Replace(
                    $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}",
                    string.Empty);
                diagnostic.MessageData = $"{shortenedFilePath}({sequencePoint.StartLine},{sequencePoint.StartColumn}): {diagnostic.MessageData}";
            }

            diagnostics.Add(diagnostic);
        }

        private static SequencePoint FindBestSequencePointFor(MethodDefinition method, Instruction instruction)
        {
            if (method == null || instruction == null)
            {
                return null;
            }

            var sequencePoints = method.DebugInformation?
                .GetSequencePointMapping()
                .Values
                .OrderBy(point => point.Offset)
                .ToArray();
            if (sequencePoints == null || sequencePoints.Length == 0)
            {
                return null;
            }

            var best = sequencePoints[0];
            foreach (var sequencePoint in sequencePoints)
            {
                if (sequencePoint.Offset > instruction.Offset)
                {
                    break;
                }

                best = sequencePoint;
            }

            return best;
        }
    }
}
