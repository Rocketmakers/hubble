using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rocketmakers.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitCodeFixProvider)), Shared]
    public class ConfigureAwaitCodeFixProvider : CodeFixProvider
    {
        private const string falseTitle = "Add ConfigureAwait(false)";

        private const string trueTitle = "Add ConfigureAwait(true)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ConfigureAwaitAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.Single();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: falseTitle,
                    createChangedDocument: c => AddConfigureAwaitAsync(context.Document, diagnostic, c, false),
                    equivalenceKey: falseTitle),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: trueTitle,
                    createChangedDocument: c => AddConfigureAwaitAsync(context.Document, diagnostic, c, true),
                    equivalenceKey: trueTitle),
                diagnostic);

            return Task.FromResult(0);
        }

        private static async Task<Document> AddConfigureAwaitAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            SyntaxKind continueOnCapturedContextSyntax;
            switch (continueOnCapturedContext)
            {
                case true:
                    continueOnCapturedContextSyntax = SyntaxKind.TrueLiteralExpression;
                    break;
                default:
                    continueOnCapturedContextSyntax = SyntaxKind.FalseLiteralExpression;
                    break;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var awaitExpression = (AwaitExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var expression = awaitExpression.Expression;

            var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    SyntaxFactory.IdentifierName(nameof(Task.ConfigureAwait))),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(continueOnCapturedContextSyntax)))));

            var newRoot = root.ReplaceNode(expression, newExpression);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
