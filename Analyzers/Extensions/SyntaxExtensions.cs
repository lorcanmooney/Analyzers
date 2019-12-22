using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

internal static class SyntaxExtensions
{
	internal static IEnumerable<Location> GetIdentifierLocations(this MemberDeclarationSyntax @this)
	{
		if (@this is MethodDeclarationSyntax method)
		{
			yield return method.Identifier.GetLocation();
		}
		else if (@this is ConstructorDeclarationSyntax constructor)
		{
			yield return constructor.Identifier.GetLocation();
		}
		else if (@this is PropertyDeclarationSyntax property)
		{
			yield return property.Identifier.GetLocation();
		}
		else if (@this is EventDeclarationSyntax @event)
		{
			yield return @event.Identifier.GetLocation();
		}
		else if (@this is IndexerDeclarationSyntax indexer)
		{
			yield return indexer.ThisKeyword.GetLocation();
		}
		else if (@this is ConversionOperatorDeclarationSyntax conversion)
		{
			yield return conversion.OperatorKeyword.GetLocation();
		}
		else if (@this is OperatorDeclarationSyntax @operator)
		{
			yield return @operator.OperatorKeyword.GetLocation();
		}
		else if (@this is DestructorDeclarationSyntax descructor)
		{
			yield return descructor.Identifier.GetLocation();
		}
		else if (@this is BaseFieldDeclarationSyntax field)
		{
			foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
			{
				yield return declarator.Identifier.GetLocation();
			}
		}
	}

	internal static Boolean HasModifier(this MemberDeclarationSyntax @this, SyntaxKind kind)
	{
		foreach (SyntaxToken token in @this.Modifiers)
		{
			if (token.IsKind(kind))
			{
				return true;
			}
		}

		return false;
	}

	internal static DocumentationCommentTriviaSyntax AddContent(this DocumentationCommentTriviaSyntax @this, IEnumerable<XmlNodeSyntax> items)
	{
		return @this.AddContent(items.ToArray());
	}

	internal static IEnumerable<ITypeSymbol> GetDocumentedExceptions(this MemberDeclarationSyntax @this, SemanticModel semanticModel)
	{
		foreach (CrefSyntax cref in @this.GetDocumentedExceptions())
		{
			if (semanticModel.GetSymbolInfo(cref).Symbol is ITypeSymbol symbol)
			{
				yield return symbol;
			}
		}
	}

	internal static MemberDeclarationSyntax GetDeclarationSyntax(this SyntaxReference @this)
	{
		return @this.GetSyntax().AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
	}

	internal static IEnumerable<CrefSyntax> GetDocumentedExceptions(this MemberDeclarationSyntax @this)
	{
		return @this
			.GetLeadingTrivia()
			.Select(trivia => trivia.GetStructure())
			.OfType<DocumentationCommentTriviaSyntax>()
			.SelectMany(GetDocumentedExceptions);
	}

	internal static IEnumerable<CrefSyntax> GetDocumentedExceptions(this DocumentationCommentTriviaSyntax @this)
	{
		return @this
			.ChildNodes()
			.OfType<XmlElementSyntax>()
			.Select(element => element.StartTag)
			.Where(tag => String.Equals("exception", tag.Name.LocalName.ValueText, StringComparison.Ordinal))
			.SelectMany(tag => tag.Attributes.OfType<XmlCrefAttributeSyntax>())
			.Select(attribute => attribute.Cref);
	}
}
