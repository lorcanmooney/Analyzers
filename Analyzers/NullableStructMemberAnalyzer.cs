using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableStructMemberAnalyzer : DiagnosticAnalyzer
{
	private static class Descriptors
	{
		private const String Category = "NullableStructMembers";

		internal static DiagnosticDescriptor ReferenceTypeFieldShouldBeNullable = new DiagnosticDescriptor(
			id: "LM1001",
			title: "Reference-type fields, auto-properties and events should be nullable when declared within a struct.",
			messageFormat: "Struct member '{0}' should be nullable.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
	{
		get
		{
			return ImmutableArray.Create(
				Descriptors.ReferenceTypeFieldShouldBeNullable
			);
		}
	}

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSymbolAction(AnalyzeField, SymbolKind.NamedType);
	}

	/// <summary>
	/// 
	/// </summary>
	/// 
	private static void AnalyzeField(SymbolAnalysisContext context)
	{
		ITypeSymbol type = ((ITypeSymbol)context.Symbol);

		if (type.IsValueType)
		{
			foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
			{
				if (!field.IsStatic && field.NullableAnnotation == NullableAnnotation.NotAnnotated && field.Type.IsReferenceType)
				{
					Diagnostic diagnostic = Diagnostic.Create(
						Descriptors.ReferenceTypeFieldShouldBeNullable,
						field.AssociatedSymbol?.Locations.FirstOrDefault() ?? field.Locations.FirstOrDefault(),
						field.AssociatedSymbol?.Name ?? field.Name
					);

					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
