using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

partial class ExceptionalAnalyzer
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fixer)), Shared]
	public class Fixer : CodeFixProvider
	{
		public sealed override ImmutableArray<String> FixableDiagnosticIds
		{
			get
			{
				return ImmutableArray.Create(
					Descriptors.DocumentThrownExceptions.Id,
					Descriptors.DocumentCalleeExceptions.Id
				);
			}
		}

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			ImmutableArray<String> exceptions = ImmutableArray<String>.Empty;

			foreach (Diagnostic diagnostic in context.Diagnostics)
			{
				if (diagnostic.Properties.TryGetValue(AnalysisContextExtensions.ExceptionMetadataName, out String exception))
				{
					exceptions = exceptions.Add(exception);
				}
			}

			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			MemberDeclarationSyntax? declaration = root?
				.FindToken(context.Diagnostics.First().Location.SourceSpan.Start)
				.Parent.AncestorsAndSelf()
				.OfType<MemberDeclarationSyntax>()
				.FirstOrDefault();

			if (!exceptions.IsDefaultOrEmpty && !(declaration is null))
			{
				CodeAction action = CodeAction.Create(
					title: "Add exception documentation",
					createChangedDocument: cancellationToken => DocumentExceptionsAsync(context.Document, declaration, exceptions),
					equivalenceKey: "DocumentExceptions"
				);

				context.RegisterCodeFix(action, context.Diagnostics);
			}
		}

		private static async Task<Document> DocumentExceptionsAsync(Document document, MemberDeclarationSyntax declaration, ImmutableArray<String> exceptions)
		{
			SyntaxNode? root = await document.GetSyntaxRootAsync();

			if (root is null)
			{
				// ?!
				return document;
			}

			DocumentationCommentTriviaSyntax? documentation = declaration
				.GetLeadingTrivia()
				.Select(trivia => trivia.GetStructure())
				.OfType<DocumentationCommentTriviaSyntax>()
				.FirstOrDefault();

			DocumentOptionSet options = await document.GetOptionsAsync();

			if (documentation is null)
			{
				root = root.ReplaceNode(
					declaration,
					declaration.WithLeadingTrivia(
						declaration
							.GetLeadingTrivia()
							.Add(Trivia(DocumentationComment(ExceptionDocumentation(options, exceptions))))
					)
				);
			}
			else
			{
				//
				// TODO: remove duplicates and subtypes that are handled by base types
				//

				root = root.ReplaceNode(
					documentation,
					documentation.AddContent(ExceptionDocumentation(options, exceptions))
				);
			}

			return document.WithSyntaxRoot(root);
		}

		private static IEnumerable<XmlNodeSyntax> ExceptionDocumentation(DocumentOptionSet options, ImmutableArray<String> exceptions)
		{
			ImmutableArrayIterator<String> iterator = new ImmutableArrayIterator<String>(exceptions);

			if (iterator.TryGetNext(out String exception, out iterator))
			{
				String newLine = options.GetOption(FormattingOptions.NewLine);

				yield return XmlExceptionElement(TypeCref(TypeName(exception)))
					.WithLeadingTrivia(DocumentationCommentExterior("/// "));

				while (iterator.TryGetNext(out exception, out iterator))
				{
					yield return XmlNewLine(newLine, continueXmlDocumentationComment: true);
					yield return XmlExceptionElement(TypeCref(TypeName(exception)));
				}

				yield return XmlNewLine(newLine, continueXmlDocumentationComment: false);
			}
		}

		private static TypeSyntax TypeName(String name)
		{
			return ParseTypeName(name).WithAdditionalAnnotations(Simplifier.Annotation);
		}

		private static XmlTextSyntax XmlNewLine(String text, Boolean continueXmlDocumentationComment) => XmlText(XmlTextNewLine(text, continueXmlDocumentationComment));

		private static DocumentationCommentTriviaSyntax DocumentationComment(IEnumerable<XmlNodeSyntax> items) => SyntaxFactory.DocumentationComment(items.ToArray());
	}
}
