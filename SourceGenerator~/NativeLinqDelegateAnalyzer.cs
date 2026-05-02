namespace KrasCore.AccumulatorGenerator
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NativeLinqDelegateAnalyzer : DiagnosticAnalyzer
    {
        private const string ATTRIBUTE_METADATA_NAME = "KrasCore.NativeDelegateMethodAttribute";

        private static readonly DiagnosticDescriptor UnsupportedLambda = new DiagnosticDescriptor(
            "KCNL001",
            "Unsupported NativeLinq delegate lambda",
            "{0}",
            "NativeLinq",
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnsupportedLambda);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
                !IsNativeDelegateOperator(method))
            {
                return;
            }

            var lambda = invocation.ArgumentList.Arguments
                .Select(a => a.Expression)
                .OfType<AnonymousFunctionExpressionSyntax>()
                .FirstOrDefault();

            if (lambda == null)
            {
                Report(context, invocation, "NativeLinq delegate operators only accept lambda expressions.");
                return;
            }

            if (lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot be async.");
            }

            if (lambda.DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Any(n => n != lambda))
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot contain nested lambdas or anonymous functions.");
            }

            if (lambda.DescendantNodes().OfType<ThisExpressionSyntax>().Any())
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot capture this.");
            }

            if (lambda.DescendantNodes().OfType<InvocationExpressionSyntax>().Any())
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot call methods in the initial unmanaged-only implementation.");
            }

            if (lambda.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any())
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot allocate or construct objects.");
            }

            if (lambda.DescendantNodes().OfType<ElementAccessExpressionSyntax>().Any())
            {
                Report(context, lambda, "NativeLinq delegate lambdas cannot use indexers or array access.");
            }

            foreach (var memberAccess in lambda.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
                if (symbol is IPropertySymbol or IEventSymbol)
                {
                    Report(context, memberAccess, "NativeLinq delegate lambdas cannot use properties, events, or indexers.");
                }
            }

            ValidateSignature(context, lambda);
            ValidateCaptures(context, lambda);
        }

        private static bool IsNativeDelegateOperator(IMethodSymbol method)
        {
            return method.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == ATTRIBUTE_METADATA_NAME);
        }

        private static void ValidateSignature(SyntaxNodeAnalysisContext context, AnonymousFunctionExpressionSyntax lambda)
        {
            if (context.SemanticModel.GetTypeInfo(lambda, context.CancellationToken).ConvertedType is not INamedTypeSymbol delegateType ||
                delegateType.DelegateInvokeMethod == null)
            {
                Report(context, lambda, "NativeLinq delegate lambda type could not be resolved.");
                return;
            }

            foreach (var parameter in delegateType.DelegateInvokeMethod.Parameters)
            {
                if (!parameter.Type.IsUnmanagedType)
                {
                    Report(context, lambda, $"NativeLinq delegate parameter '{parameter.Name}' must be unmanaged.");
                }
            }

            var returnType = delegateType.DelegateInvokeMethod.ReturnType;
            if (returnType.SpecialType != SpecialType.System_Void && !returnType.IsUnmanagedType)
            {
                Report(context, lambda, "NativeLinq delegate return type must be unmanaged.");
            }

            if (lambda is ParenthesizedLambdaExpressionSyntax parenthesized)
            {
                foreach (var parameter in parenthesized.ParameterList.Parameters)
                {
                    if (parameter.Modifiers.Any(SyntaxKind.RefKeyword) ||
                        parameter.Modifiers.Any(SyntaxKind.OutKeyword))
                    {
                        Report(context, parameter, "NativeLinq delegate lambda parameters cannot be ref or out parameters.");
                    }
                }
            }
            else
            {
                Report(context, lambda, "NativeLinq delegate lambdas must use an explicit parameter list.");
            }
        }

        private static void ValidateCaptures(SyntaxNodeAnalysisContext context, AnonymousFunctionExpressionSyntax lambda)
        {
            var dataFlow = lambda.Body is CSharpSyntaxNode body
                ? context.SemanticModel.AnalyzeDataFlow(body)
                : default;

            var captured = dataFlow != null && dataFlow.Succeeded
                ? dataFlow.DataFlowsIn.Concat(dataFlow.CapturedInside).Distinct(SymbolEqualityComparer.Default).ToArray()
                : System.Array.Empty<ISymbol>();

            var isStatic = lambda switch
            {
                ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.Modifiers.Any(SyntaxKind.StaticKeyword),
                SimpleLambdaExpressionSyntax simple => simple.Modifiers.Any(SyntaxKind.StaticKeyword),
                _ => false,
            };

            if (isStatic)
            {
                return;
            }

            if (captured.Length == 0)
            {
                Report(context, lambda, "NativeLinq delegate lambdas without captures must be static.");
                return;
            }

            foreach (var symbol in captured)
            {
                if (symbol is ILocalSymbol local)
                {
                    ValidateCapturedType(context, lambda, local.Name, local.Type);
                }
                else if (symbol is IParameterSymbol parameter)
                {
                    ValidateCapturedType(context, lambda, parameter.Name, parameter.Type);
                }
                else
                {
                    Report(context, lambda, $"NativeLinq delegate capture '{symbol.Name}' is not a local unmanaged value.");
                }
            }
        }

        private static void ValidateCapturedType(SyntaxNodeAnalysisContext context, SyntaxNode location, string name, ITypeSymbol type)
        {
            if (!type.IsUnmanagedType)
            {
                Report(context, location, $"NativeLinq delegate capture '{name}' must be unmanaged.");
            }
        }

        private static void Report(SyntaxNodeAnalysisContext context, SyntaxNode node, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(UnsupportedLambda, node.GetLocation(), message));
        }
    }
}
