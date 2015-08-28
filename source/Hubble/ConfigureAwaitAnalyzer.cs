using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hubble
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConfigureAwaitAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private const string title = "ConfigureAwait not called";
        private const string messageFormat = "await keyword used without first calling ConfigureAwait";
        private const string category = "Syntax";

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(DiagnosticId, title, messageFormat, category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpressionSyntax = (AwaitExpressionSyntax)context.Node;
            var expression = awaitExpressionSyntax.Expression;

            var type = context.SemanticModel.GetTypeInfo(expression).Type as INamedTypeSymbol;
            if (type == null)
            {
                return;
            }

            Type taskType;
            if (type.IsGenericType)
            {
                type = type.ConstructedFrom;
                taskType = typeof(ConfiguredTaskAwaitable<>);
            }
            else
            {
                taskType = typeof(ConfiguredTaskAwaitable);
            }

            if (!type.Equals(context.SemanticModel.Compilation.GetTypeByMetadataName(taskType.FullName)))
            {
                var diagnostic = Diagnostic.Create(rule, awaitExpressionSyntax.AwaitKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
