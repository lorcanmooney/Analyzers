using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadOnlyStructAnalyzer : DiagnosticAnalyzer
{
	public const String DiagnosticId = "LM2001";

	private const String Category = "ReadOnlyStructs";

	private static DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
		id: DiagnosticId,
		title: "Immutable structs fields should be readonly.",
		messageFormat: "Struct '{0}' is immutable and should be readonly.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
	{
		get
		{
			return ImmutableArray.Create(Descriptor);
		}
	}

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSymbolAction(AnalyzeStruct, SymbolKind.NamedType);
	}

	private static void AnalyzeStruct(SymbolAnalysisContext context)
	{
		INamedTypeSymbol type = ((INamedTypeSymbol)context.Symbol);

		if ((type.TypeKind == TypeKind.Struct) && !type.IsReadOnly && IsImmutable(type))
		{
			Diagnostic diagnostic = Diagnostic.Create(Descriptor, type.Locations[0], type.Name);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private static Boolean IsImmutable(INamedTypeSymbol type)
	{
		foreach (ISymbol member in type.GetMembers())
		{
			if ((member is IFieldSymbol field) && !field.IsStatic && !field.IsReadOnly)
			{
				return false;
			}
			else if ((member is IEventSymbol @event))
			{
				//
				// HACK: Compiler-generated backing fields for events are not surfaced as symbols, so fall back to the syntax model
				//       See https://github.com/dotnet/roslyn/issues/36259
				//
				Boolean isFieldEvent = @event
					.DeclaringSyntaxReferences
					.Select(
						reference => reference
							.GetSyntax()
							.AncestorsAndSelf()
							.OfType<EventFieldDeclarationSyntax>()
							.FirstOrDefault()
					)
					.WhereNotNull()
					.Any();

				if (isFieldEvent)
				{
					// Compiler-generated event fields are always mutable
					return false;
				}
			}
		}

		return true;
	}
}
