namespace KrasCore.AccumulatorGenerator
{
    using System.Collections.Generic;
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

            var delegateArguments = invocation.ArgumentList.Arguments
                .Where(argument => IsDelegateExpression(context, argument.Expression))
                .ToArray();
            if (delegateArguments.Length == 0)
            {
                Report(context, invocation, "NativeLinq delegate operators require a lambda or method group delegate argument.");
                return;
            }

            foreach (var delegateArgument in delegateArguments)
            {
                ValidateDelegateArgument(context, delegateArgument.Expression);
            }
        }

        private static void ValidateDelegateArgument(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression is not AnonymousFunctionExpressionSyntax lambda)
            {
                ValidateMethodGroup(context, expression);
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

            ValidateSignature(context, lambda);
            ValidateBody(context, lambda.Body, lambda, context.ContainingSymbol?.ContainingType);
            ValidateCaptures(context, lambda);
        }

        private static bool IsDelegateExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            return context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).ConvertedType is INamedTypeSymbol
            {
                DelegateInvokeMethod: not null,
            };
        }

        private static bool IsNativeDelegateOperator(IMethodSymbol method)
        {
            return HasNativeDelegateMethodAttribute(method) ||
                HasNativeDelegateMethodAttribute(method.OriginalDefinition) ||
                HasNativeDelegateMethodAttribute(method.ReducedFrom) ||
                HasNativeDelegateMethodAttribute(method.ReducedFrom?.OriginalDefinition);
        }

        private static bool HasNativeDelegateMethodAttribute(IMethodSymbol? method)
        {
            return method?.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == ATTRIBUTE_METADATA_NAME) == true;
        }

        private static void ValidateMethodGroup(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
            var method = symbolInfo.Symbol as IMethodSymbol ??
                symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            if (method == null)
            {
                Report(context, expression, "NativeLinq delegate method group could not be resolved.");
                return;
            }

            ValidateMethodSignature(context, expression, method);

            if (!method.IsStatic)
            {
                var receiverType = expression is MemberAccessExpressionSyntax memberAccess
                    ? context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type
                    : method.ContainingType;
                if (receiverType != null && !receiverType.IsUnmanagedType)
                {
                    Report(context, expression, $"NativeLinq delegate target '{receiverType.ToDisplayString()}' must be unmanaged.");
                }
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax(context.CancellationToken);
                switch (syntax)
                {
                    case MethodDeclarationSyntax methodDeclaration:
                        ValidateMethodBody(context, methodDeclaration, method);
                        return;
                    case LocalFunctionStatementSyntax localFunction:
                        ValidateLocalFunctionBody(context, localFunction, method);
                        return;
                }
            }
        }

        private static void ValidateSignature(SyntaxNodeAnalysisContext context, AnonymousFunctionExpressionSyntax lambda)
        {
            if (context.SemanticModel.GetTypeInfo(lambda, context.CancellationToken).ConvertedType is not INamedTypeSymbol delegateType ||
                delegateType.DelegateInvokeMethod == null)
            {
                Report(context, lambda, "NativeLinq delegate lambda type could not be resolved.");
                return;
            }

            ValidateUnmanagedSignature(
                context,
                lambda,
                delegateType.DelegateInvokeMethod.Parameters,
                delegateType.DelegateInvokeMethod.ReturnType,
                "NativeLinq delegate parameter '{0}' must be unmanaged.",
                "NativeLinq delegate return type must be unmanaged.",
                false,
                null);

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
        }

        private static void ValidateMethodSignature(SyntaxNodeAnalysisContext context, SyntaxNode location, IMethodSymbol method)
        {
            ValidateUnmanagedSignature(
                context,
                location,
                method.Parameters,
                method.ReturnType,
                "NativeLinq delegate parameter '{0}' must be unmanaged.",
                "NativeLinq delegate return type must be unmanaged.",
                true,
                "NativeLinq delegate method parameters cannot be ref or out parameters.");
        }

        private static void ValidateMethodBody(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol method)
        {
            if (methodDeclaration.Body != null)
            {
                ValidateBody(context, methodDeclaration.Body, methodDeclaration, method.ContainingType);
            }

            if (methodDeclaration.ExpressionBody?.Expression != null)
            {
                ValidateBody(context, methodDeclaration.ExpressionBody.Expression, methodDeclaration.ExpressionBody.Expression, method.ContainingType);
            }
        }

        private static void ValidateLocalFunctionBody(SyntaxNodeAnalysisContext context, LocalFunctionStatementSyntax localFunction, IMethodSymbol method)
        {
            if (localFunction.Body != null)
            {
                ValidateBody(context, localFunction.Body, localFunction, method.ContainingType);
            }

            if (localFunction.ExpressionBody?.Expression != null)
            {
                ValidateBody(context, localFunction.ExpressionBody.Expression, localFunction.ExpressionBody.Expression, method.ContainingType);
            }
        }

        private static void ValidateBody(
            SyntaxNodeAnalysisContext context,
            SyntaxNode body,
            SyntaxNode diagnosticLocation,
            ITypeSymbol? thisType)
        {
            foreach (var nestedLambda in body.DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>())
            {
                Report(context, nestedLambda, "NativeLinq delegate bodies cannot contain nested lambdas or anonymous functions.");
            }

            foreach (var thisExpression in body.DescendantNodes().OfType<ThisExpressionSyntax>())
            {
                if (thisType == null || !thisType.IsUnmanagedType)
                {
                    Report(context, thisExpression, "NativeLinq delegate bodies can only use this when the containing type is unmanaged.");
                }
            }

            foreach (var literal in body.DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                if (literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    Report(context, literal, "NativeLinq delegate bodies cannot use managed string literals.");
                }
            }

            foreach (var invocationExpression in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                ValidateInvocation(context, invocationExpression);
            }

            foreach (var objectCreation in body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var createdType = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken).Type;
                if (createdType == null || !createdType.IsUnmanagedType)
                {
                    Report(context, objectCreation, "NativeLinq delegate bodies cannot create managed objects.");
                }
            }

            foreach (var arrayCreation in body.DescendantNodes().OfType<ArrayCreationExpressionSyntax>())
            {
                Report(context, arrayCreation, "NativeLinq delegate bodies cannot create managed arrays.");
            }

            foreach (var implicitArrayCreation in body.DescendantNodes().OfType<ImplicitArrayCreationExpressionSyntax>())
            {
                Report(context, implicitArrayCreation, "NativeLinq delegate bodies cannot create managed arrays.");
            }

            foreach (var elementAccess in body.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                var receiverType = context.SemanticModel.GetTypeInfo(elementAccess.Expression, context.CancellationToken).Type;
                if (receiverType?.SpecialType == SpecialType.System_String)
                {
                    if (!elementAccess.Expression.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        Report(context, elementAccess.Expression, "NativeLinq delegate bodies cannot use managed strings.");
                    }
                }
                else if (receiverType == null || !receiverType.IsUnmanagedType)
                {
                    Report(context, elementAccess, "NativeLinq delegate bodies cannot use managed indexers or array access.");
                }
            }

            foreach (var local in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (context.SemanticModel.GetDeclaredSymbol(local, context.CancellationToken) is ILocalSymbol localSymbol &&
                    localSymbol.Type.SpecialType != SpecialType.System_Void &&
                    !localSymbol.Type.IsUnmanagedType)
                {
                    Report(context, local, $"NativeLinq delegate local '{localSymbol.Name}' must be unmanaged.");
                }
            }

            foreach (var memberAccess in body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
                if (symbol is IEventSymbol)
                {
                    Report(context, memberAccess, "NativeLinq delegate bodies cannot use events.");
                }
                else if (symbol is IPropertySymbol property)
                {
                    ValidatePropertyUse(context, memberAccess, property);
                }
            }
        }

        private static void ValidateInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpression, context.CancellationToken);
            var method = symbolInfo.Symbol as IMethodSymbol ??
                symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            if (method == null)
            {
                Report(context, invocationExpression, "NativeLinq delegate method call could not be resolved.");
                return;
            }

            ValidateMethodUse(context, invocationExpression, method);
        }

        private static void ValidateMethodUse(SyntaxNodeAnalysisContext context, SyntaxNode location, IMethodSymbol method)
        {
            if (!method.IsStatic && method.ContainingType != null && !method.ContainingType.IsUnmanagedType)
            {
                Report(context, location, $"NativeLinq delegate bodies cannot call instance methods on managed type '{method.ContainingType.ToDisplayString()}'.");
            }

            ValidateUnmanagedSignature(
                context,
                location,
                method.Parameters,
                method.ReturnType,
                "NativeLinq delegate method parameter '{0}' must be unmanaged.",
                "NativeLinq delegate method return type must be unmanaged.",
                false,
                null);

            foreach (var typeArgument in method.TypeArguments)
            {
                if (!typeArgument.IsUnmanagedType)
                {
                    Report(context, location, $"NativeLinq delegate generic argument '{typeArgument.ToDisplayString()}' must be unmanaged.");
                }
            }
        }

        private static void ValidatePropertyUse(SyntaxNodeAnalysisContext context, SyntaxNode location, IPropertySymbol property)
        {
            if (!property.IsStatic && property.ContainingType != null && !property.ContainingType.IsUnmanagedType)
            {
                Report(context, location, $"NativeLinq delegate bodies cannot use properties on managed type '{property.ContainingType.ToDisplayString()}'.");
            }

            if (!property.Type.IsUnmanagedType)
            {
                Report(context, location, $"NativeLinq delegate property '{property.Name}' must return an unmanaged type.");
            }
        }

        private static void ValidateUnmanagedSignature(
            SyntaxNodeAnalysisContext context,
            SyntaxNode location,
            IEnumerable<IParameterSymbol> parameters,
            ITypeSymbol returnType,
            string parameterMessageFormat,
            string returnMessage,
            bool rejectRefOutParameters,
            string? refOutMessage)
        {
            foreach (var parameter in parameters)
            {
                if (!parameter.Type.IsUnmanagedType)
                {
                    Report(context, location, string.Format(parameterMessageFormat, parameter.Name));
                }

                if (rejectRefOutParameters &&
                    (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out))
                {
                    Report(context, location, refOutMessage!);
                }
            }

            if (returnType.SpecialType != SpecialType.System_Void && !returnType.IsUnmanagedType)
            {
                Report(context, location, returnMessage);
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
                else if (symbol is IFieldSymbol field)
                {
                    if (!field.IsStatic &&
                        field.ContainingType != null &&
                        !field.ContainingType.IsUnmanagedType)
                    {
                        Report(context, lambda, $"NativeLinq delegate capture '{field.Name}' is on managed type '{field.ContainingType.ToDisplayString()}'.");
                    }

                    ValidateCapturedType(context, lambda, field.Name, field.Type);
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
