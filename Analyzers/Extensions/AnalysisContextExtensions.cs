using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

internal static class AnalysisContextExtensions
{
	internal const String ExceptionMetadataName = nameof(ExceptionMetadataName);

	internal static void ReportIncompatibleExceptions(
		this SymbolAnalysisContext @this,
		DiagnosticDescriptor descriptor,
		IEnumerable<CrefSyntax> exceptions,
		ISymbol symbol
	)
	{
		foreach (CrefSyntax exception in exceptions)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				descriptor,
				exception.GetLocation(),
				exception.ToString(),
				symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
			);

			@this.ReportDiagnostic(diagnostic);
		}
	}

	internal static void ReportUndocumentedExceptions(
		this OperationAnalysisContext @this,
		DiagnosticDescriptor descriptorForThrow,
		DiagnosticDescriptor descriptorForInvocation,
		TypeSet documented,
		IEnumerable<ExceptionalOperation> exceptions
	)
	{
		foreach (ExceptionalOperation exception in exceptions)
		{
			Boolean isDocumented = documented.Contains(exception.Type);

			if (!isDocumented)
			{
				@this.ReportUndocumentedException(
					descriptorForThrow: descriptorForThrow,
					descriptorForInvocation: descriptorForInvocation,
					exception: exception
				);
			}
		}
	}

	internal static void ReportUndocumentedException(
		this OperationAnalysisContext @this,
		DiagnosticDescriptor descriptorForThrow,
		DiagnosticDescriptor descriptorForInvocation,
		ExceptionalOperation exception
	)
	{
		String name = exception.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
		Location location = exception.Location;
		IMethodSymbol? method = exception.Method;

		//
		// TODO: ExceptionMetadataName should be in cref format
		//
		ImmutableDictionary<String, String> properties = ImmutableDictionary<String, String>.Empty
			.Add(ExceptionMetadataName, exception.Type.Name);

		Diagnostic diagnostic;

		if (method is null)
		{
			diagnostic = Diagnostic.Create(descriptorForThrow, location, properties, name);
		}
		else
		{
			diagnostic = Diagnostic.Create(descriptorForInvocation, location, properties, name, method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
		}

		@this.ReportDiagnostic(diagnostic);
	}

	//internal static void ReportUndocumentedExceptions(
	//	this OperationAnalysisContext @this,
	//	DiagnosticDescriptor descriptorForThrow,
	//	DiagnosticDescriptor descriptorForInvocation,
	//	CheckedExceptions documented,
	//	IEnumerable<ExceptionalOperation> exceptions,
	//	MemberDeclarationSyntax declaration,
	//	ISymbol member
	//)
	//{
	//	foreach (ExceptionalOperation exception in exceptions)
	//	{
	//		Boolean isDocumented = documented.IsHandled(exception.Type);

	//		if (!isDocumented)
	//		{
	//			String name = exception.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
	//			Location location = exception.Location;
	//			IMethodSymbol? method = exception.Method;

	//			//
	//			// TODO: ExceptionMetadataName should be in cref format
	//			//
	//			ImmutableDictionary<String, String> properties = ImmutableDictionary<String, String>.Empty
	//				.Add(ExceptionMetadataName, exception.Type.ToMinimalDisplayString(@this.Operation.SemanticModel, position: declaration.SpanStart));

	//			Diagnostic diagnostic;

	//			if (method is null)
	//			{
	//				diagnostic = Diagnostic.Create(
	//					descriptorForThrow,
	//					location,
	//					properties,
	//					name,
	//					member.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
	//				);
	//			}
	//			else
	//			{
	//				diagnostic = Diagnostic.Create(
	//					descriptorForInvocation,
	//					location,
	//					properties,
	//					name,
	//					member.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
	//					method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
	//				);
	//			}

	//			@this.ReportDiagnostic(diagnostic);
	//		}
	//	}
	//}
}
