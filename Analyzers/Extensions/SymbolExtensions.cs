using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

internal static class SymbolExtensions
{
	internal static Boolean IsAssignableTo(this ITypeSymbol @this, ITypeSymbol @base)
	{
		ITypeSymbol? type = @this;

		while (!(type is null))
		{
			if (SymbolEqualityComparer.Default.Equals(type, @base))
			{
				return true;
			}
			else
			{
				type = type.BaseType;
			}
		}

		return false;
	}

	internal static IEnumerable<IMethodSymbol> GetInstanceInitializationConstructors(this INamedTypeSymbol @this)
	{
		return @this.Constructors.Where(IsInstanceInitializingConstructor);
	}

	private static Boolean IsInstanceInitializingConstructor(IMethodSymbol method)
	{
		foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
		{
			if (reference.GetDeclarationSyntax() is ConstructorDeclarationSyntax declaration)
			{
				if (declaration.Initializer is null)
				{
					return true;
				}
				else if (
					(declaration.Initializer is ConstructorInitializerSyntax initializer) &&
					(initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword))
				)
				{
					return true;
				}
			}
		}

		return false;
	}

	internal static ISymbol? GetOverriddenMember(this ISymbol @this)
	{
		if (@this is IMethodSymbol method)
		{
			return method.OverriddenMethod;
		}
		else if (@this is IPropertySymbol property)
		{
			return property.OverriddenProperty;
		}
		else if (@this is IEventSymbol @event)
		{
			return @event.OverriddenEvent;
		}
		else
		{
			return null;
		}
	}

	internal static IMethodSymbol? GetParameterlessInstanceConstructor(this INamedTypeSymbol @this)
	{
		return @this.InstanceConstructors.Where(constructor => constructor.Parameters.IsEmpty).FirstOrDefault();
	}

	internal static TypeSet GetDocumentedExceptionSet(this ISymbol @this, Compilation compilation)
	{
		return TypeSet.Empty.Add(
			compilation.GetTypesByMetadataName(@this.GetDocumentedExceptionsByMetadataName())
		);
	}

	internal static IEnumerable<INamedTypeSymbol> GetDocumentedExceptions(this ISymbol @this, Compilation compilation)
	{
		return compilation.GetTypesByMetadataName(@this.GetDocumentedExceptionsByMetadataName());
	}

	private static IEnumerable<String> GetDocumentedExceptionsByMetadataName(this ISymbol @this)
	{
		String? documentation = @this.GetDocumentationCommentXml();

		if (documentation is null)
		{
			return Enumerable.Empty<String>();
		}
		else
		{
			try
			{
				//
				// TODO: decode crefs
				//
				return XElement
					.Parse(documentation)
					.Elements("exception")
					.Select(element => element.Attribute("cref")?.Value)
					.Where(cref => ((cref is String) && cref.StartsWith("T:", StringComparison.OrdinalIgnoreCase)))
					.Select(cref => cref!.Substring(2));
			}
			catch (XmlException)
			{
				return Enumerable.Empty<String>();
			}
		}
	}
}
