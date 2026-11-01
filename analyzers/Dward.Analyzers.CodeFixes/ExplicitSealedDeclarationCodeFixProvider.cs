using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dward.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExplicitSealedDeclarationCodeFixProvider)), Shared]
public class ExplicitSealedDeclarationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove explicit 'sealed' modifier";

    public override ImmutableArray<string> FixableDiagnosticIds => ["DWARD0004"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var modifier = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!modifier.IsKind(SyntaxKind.SealedKeyword)
                || modifier.Span != diagnostic.Location.SourceSpan
                || modifier.Parent is not TypeDeclarationSyntax declaration
                || (!declaration.IsKind(SyntaxKind.ClassDeclaration) && !declaration.IsKind(SyntaxKind.RecordDeclaration))
                || declaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                continue;
            }

            var action = CodeAction.Create(
                Title,
                cancellationToken => RemoveSealedModifier(context.Document, declaration, modifier, cancellationToken),
                equivalenceKey: Title);
            context.RegisterCodeFix(action, diagnostic);
        }
    }

    private static async Task<Document> RemoveSealedModifier(
        Document document,
        TypeDeclarationSyntax declaration,
        SyntaxToken modifier,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cannot remove a modifier from a document without a syntax root.");
        var modifierIndex = declaration.Modifiers.IndexOf(modifier);
        var nextToken = modifier.GetNextToken();
        var trailingTrivia = modifier.TrailingTrivia.SkipWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));
        var preservedTrivia = modifier.LeadingTrivia.AddRange(trailingTrivia).AddRange(nextToken.LeadingTrivia);
        var replacement = declaration.ReplaceToken(nextToken, nextToken.WithLeadingTrivia(preservedTrivia));
        replacement = replacement.WithModifiers(replacement.Modifiers.RemoveAt(modifierIndex));

        var changedRoot = root.ReplaceNode(declaration, replacement);
        return document.WithSyntaxRoot(changedRoot);
    }
}
