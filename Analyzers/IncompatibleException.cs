using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal readonly struct IncompatibleException
{
	internal readonly ITypeSymbol Type;
	internal readonly XmlNodeSyntax Syntax;

	public IncompatibleException(ITypeSymbol type, CrefSyntax cref)
	{
		Type = type;
		Syntax = cref;
	}
}
