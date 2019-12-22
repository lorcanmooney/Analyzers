using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class ExceptionalAnalyzer : DiagnosticAnalyzer
{
	private static class Descriptors
	{
		private const String Category = "CheckedExceptions";

		//
		// TODO: distill all this nonsense to something smaller
		//
		internal static readonly DiagnosticDescriptor DocumentThrownExceptions = new DiagnosticDescriptor(
			id: "LM3001",
			title: "Thrown exceptions should be documented.",
			messageFormat: "Exception '{0}' should be documented.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor DocumentCalleeExceptions = new DiagnosticDescriptor(
			id: "LM3002",
			title: "Exceptions thrown by callees should be documented.",
			messageFormat: "Exception '{0}', thrown by '{1}', should be documented.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor DocumentInstanceMemberInitializerExceptions = new DiagnosticDescriptor(
			id: "LM3010",
			title: "Exceptions thrown by instance member initializers should be documented by all initializing constructors.",
			messageFormat: "Exception '{0}', thrown by initializer for '{1}', should be documented.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor DocumentInstanceMemberInitializerThrownExceptions = new DiagnosticDescriptor(
			id: "LM3010",
			title: "Exceptions thrown by instance member initializers should be documented by all initializing constructors.",
			messageFormat: "Exception '{0}' should be documented by all initializing constructors.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor DocumentInstanceMemberInitializerCalleeExceptions = new DiagnosticDescriptor(
			id: "LM3011",
			title: "Exceptions thrown by instance member initializers should be documented by all initializing constructors.",
			messageFormat: "Exception '{0}', thrown by '{1}', should be documented by all initializing constructors.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor StaticConstructorsShouldNotThrowExceptions = new DiagnosticDescriptor(
			id: "LM3005",
			title: "Static constructors should not throw exceptions.",
			messageFormat: "Exceptions should not be thrown by static constructors.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor StaticInitializersShouldNotThrowExceptions = new DiagnosticDescriptor(
			id: "LM3005",
			title: "Static initializers should not throw exceptions.",
			messageFormat: "Exceptions should not be thrown by static initializers.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor StaticConstructorsShouldCatchCalleeExceptions = new DiagnosticDescriptor(
			id: "LM3006",
			title: "Static constructors should not throw exceptions.",
			messageFormat: "Exception '{0}', thrown by '{1}', should be caught.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor StaticMemberInitializersShouldNotThrowExceptions = new DiagnosticDescriptor(
			id: "LM3009",
			title: "Static member initializers should not throw exceptions.",
			messageFormat: "Static member initializers should not throw exceptions.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor OverriddenMembersShouldThrowCompatibleExceptions = new DiagnosticDescriptor(
			id: "LM3003",
			title: "Exceptions thrown by method overrides must be compatible with the base method.",
			messageFormat: "Exception '{0}' is not compatible with any exception documented by '{1}'.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		internal static readonly DiagnosticDescriptor InterfaceImplementationsShouldThrowCompatibleExceptions = new DiagnosticDescriptor(
			id: "LM3004",
			title: "Exceptions thrown by interface implementations must be compatible with the interface.",
			messageFormat: "Exception '{0}' is not compatible with any exception documented by '{1}'.",
			category: Category,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);
	}

	public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
	{
		get
		{
			return ImmutableArray.Create(
				// Fixable scenarios
				Descriptors.DocumentThrownExceptions,
				Descriptors.DocumentCalleeExceptions,
				Descriptors.DocumentInstanceMemberInitializerExceptions,
				Descriptors.DocumentInstanceMemberInitializerThrownExceptions,
				Descriptors.DocumentInstanceMemberInitializerCalleeExceptions,

				// Forbidden scenarios
				Descriptors.StaticConstructorsShouldNotThrowExceptions,
				Descriptors.StaticConstructorsShouldCatchCalleeExceptions,
				Descriptors.StaticMemberInitializersShouldNotThrowExceptions,
				Descriptors.StaticInitializersShouldNotThrowExceptions,

				// Contract enforcement
				Descriptors.OverriddenMembersShouldThrowCompatibleExceptions,
				Descriptors.InterfaceImplementationsShouldThrowCompatibleExceptions
			);
		}
	}

	public sealed override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSymbolAction(AnalyzeInterfaceImplementations, SymbolKind.NamedType);

		context.RegisterSymbolAction(AnalyzeOverriddenMember, SymbolKind.Method, SymbolKind.Property, SymbolKind.Event);

		context.RegisterOperationAction(AnalyzeConstructorBody, OperationKind.ConstructorBody);
		context.RegisterOperationAction(AnalyzeMethodBody, OperationKind.MethodBody);
		context.RegisterOperationAction(AnalyzeSymbolInitializer, OperationKind.FieldInitializer, OperationKind.PropertyInitializer);

		//
		// TODO: destructors?
		//

		//
		// TODO: verify declared exceptions on conversions between delegate types
		//
	}

	private static void AnalyzeInterfaceImplementations(SymbolAnalysisContext context)
	{
		INamedTypeSymbol implementationType = ((INamedTypeSymbol)context.Symbol);

		foreach (INamedTypeSymbol interfaceType in implementationType.Interfaces)
		{
			foreach (ISymbol interfaceMember in interfaceType.GetMembers())
			{
				if ((interfaceMember is IMethodSymbol interfaceMethod) && !(interfaceMethod.AssociatedSymbol is null))
				{
					// Filter out methods used to implement properties and events
					continue;
				}

				ISymbol? implementationMember = implementationType.FindImplementationForInterfaceMember(interfaceMember);

				if (!(implementationMember is null))
				{
					context.ReportIncompatibleExceptions(
						descriptor: Descriptors.InterfaceImplementationsShouldThrowCompatibleExceptions,
						exceptions: GetIncompatibleExceptions(context.Compilation, interfaceMember, implementationMember),
						symbol: interfaceMember
					);
				}
			}
		}
	}

	private static void AnalyzeOverriddenMember(SymbolAnalysisContext context)
	{
		Compilation compilation = context.Compilation;

		ISymbol overridingMember = context.Symbol;

		if (overridingMember.GetOverriddenMember() is ISymbol overriddenMember)
		{
			context.ReportIncompatibleExceptions(
				descriptor: Descriptors.OverriddenMembersShouldThrowCompatibleExceptions,
				exceptions: GetIncompatibleExceptions(compilation, overriddenMember, overridingMember),
				symbol: overridingMember
			);
		}
	}

	private void AnalyzeSymbolInitializer(OperationAnalysisContext context)
	{
		ISymbolInitializerOperation operation = ((ISymbolInitializerOperation)context.Operation);
		Compilation compilation = context.Compilation;

		ISymbol initializedMember = operation.GetInitializedMembers().First();
		IEnumerable<ExceptionalOperation> initializerExceptions = operation.GetExceptionalOperations(compilation);

		if (initializedMember.IsStatic)
		{
			foreach (ExceptionalOperation initializerException in initializerExceptions)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					Descriptors.StaticInitializersShouldNotThrowExceptions,
					initializerException.Location
				));
			}
		}
		else
		{
			IEnumerable<IMethodSymbol> initializationConstructors = operation.GetInitializationConstructors();

			TypeSet documentedExceptionsInAllInitializationConstructors = TypeSet.Universal;

			foreach (IMethodSymbol constructor in initializationConstructors)
			{
				TypeSet documentedExceptions = constructor.GetDocumentedExceptionSet(compilation);

				documentedExceptionsInAllInitializationConstructors &= documentedExceptions;

				foreach (ExceptionalOperation exception in initializerExceptions)
				{
					ITypeSymbol exceptionType = exception.Type;

					if (!documentedExceptions.Contains(exceptionType))
					{
						foreach (Location location in constructor.Locations)
						{
							context.ReportDiagnostic(Diagnostic.Create(
								Descriptors.DocumentInstanceMemberInitializerExceptions,
								location,
								exceptionType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
								initializedMember.Name
							));
						}
					}
				}
			}

			// For each exception that isn't documented in all the right places, add an additional diagnostic at the throw/invocation site
			foreach (ExceptionalOperation exception in initializerExceptions)
			{
				Boolean documentedInAllInitializationConstructors =
					documentedExceptionsInAllInitializationConstructors.Contains(exception.Type);

				if (!documentedInAllInitializationConstructors)
				{
					context.ReportUndocumentedException(
						descriptorForThrow: Descriptors.DocumentInstanceMemberInitializerThrownExceptions,
						descriptorForInvocation: Descriptors.DocumentInstanceMemberInitializerCalleeExceptions,
						exception: exception
					);
				}
			}
		}
	}

	private static void AnalyzeConstructorBody(OperationAnalysisContext context)
	{
		IConstructorBodyOperation operation = ((IConstructorBodyOperation)context.Operation);
		Compilation compilation = context.Compilation;

		TypeSet exceptions = GetIgnoredExceptionSet(compilation);

		IMethodSymbol symbol = operation.GetSymbol();

		if (symbol.IsStatic)
		{
			foreach (CrefSyntax cref in operation.GetDeclarationSyntax().GetDocumentedExceptions())
			{
				Diagnostic diagnostic = Diagnostic.Create(
					descriptor: Descriptors.StaticConstructorsShouldNotThrowExceptions,
					location: cref.Ancestors().OfType<XmlNodeSyntax>().FirstOrDefault()?.GetLocation()
				);

				context.ReportDiagnostic(diagnostic);
			}

			context.ReportUndocumentedExceptions(
				descriptorForThrow: Descriptors.StaticConstructorsShouldNotThrowExceptions,
				descriptorForInvocation: Descriptors.StaticConstructorsShouldCatchCalleeExceptions,
				documented: exceptions,
				exceptions: operation.GetExceptionalOperations(compilation)
			);
		}
		else
		{
			exceptions = exceptions.Add(operation.GetDocumentedExceptions());

			context.ReportUndocumentedExceptions(
				descriptorForThrow: Descriptors.DocumentThrownExceptions,
				descriptorForInvocation: Descriptors.DocumentCalleeExceptions,
				documented: exceptions,
				exceptions: operation.GetExceptionalOperations(compilation)
			);

			if (operation.Initializer is null)
			{
				//
				// HACK: Implicit/compiler-generated calls to parameterless base-class constructors are
				//       not included in the operation hierarchy and need to be handled explicitly.
				//
				if (
					(symbol is IMethodSymbol constructor) && !constructor.IsStatic &&
					(constructor.ContainingType is INamedTypeSymbol type) && (type.TypeKind == TypeKind.Class) &&
					(type.BaseType is INamedTypeSymbol baseType) &&
					(baseType.GetParameterlessInstanceConstructor() is IMethodSymbol baseParameterlessConstructor)
				)
				{
					Location location = operation.GetDeclarationSyntax().GetIdentifierLocations().First();

					//
					// TODO: better diagnostic message which refers to the implicit parameterless constructor call
					//
					context.ReportUndocumentedExceptions(
						descriptorForThrow: Descriptors.DocumentThrownExceptions,
						descriptorForInvocation: Descriptors.DocumentCalleeExceptions,
						documented: exceptions,
						exceptions: baseParameterlessConstructor
							.GetDocumentedExceptions(compilation)
							.Select(exception => new ExceptionalOperation(location, baseParameterlessConstructor, exception))
					);
				}
			}
		}
	}

	private static void AnalyzeMethodBody(OperationAnalysisContext context)
	{
		IMethodBodyBaseOperation operation = ((IMethodBodyBaseOperation)context.Operation);
		Compilation compilation = context.Compilation;

		TypeSet exceptions = TypeSet.Empty
			.Add(GetIgnoredExceptions(compilation))
			.Add(operation.GetDocumentedExceptions());

		context.ReportUndocumentedExceptions(
			descriptorForThrow: Descriptors.DocumentThrownExceptions,
			descriptorForInvocation: Descriptors.DocumentCalleeExceptions,
			documented: exceptions,
			exceptions: operation.GetExceptionalOperations(compilation)
		);
	}

	private static IEnumerable<CrefSyntax> GetIncompatibleExceptions(Compilation compilation, ISymbol allowed, ISymbol candidate)
	{
		ImmutableArray<INamedTypeSymbol> allowedExceptions = allowed.GetDocumentedExceptions(compilation).ToImmutableArray();

		foreach (SyntaxReference reference in candidate.DeclaringSyntaxReferences)
		{
			foreach (CrefSyntax exception in reference.GetDeclarationSyntax().GetDocumentedExceptions())
			{
				SemanticModel semanticModel = compilation.GetSemanticModel(exception.SyntaxTree);

				if (semanticModel.GetSymbolInfo(exception).Symbol is ITypeSymbol candidateException)
				{
					Boolean compatible = false;

					foreach (ITypeSymbol baseMethodException in allowedExceptions)
					{
						if (candidateException.IsAssignableTo(baseMethodException))
						{
							compatible = true;
							break;
						}
					}

					if (!compatible)
					{
						yield return exception;
					}
				}
			}
		}
	}

	private static TypeSet GetIgnoredExceptionSet(Compilation compilation) => TypeSet.Empty.Add(GetIgnoredExceptions(compilation));

	private static IEnumerable<ITypeSymbol> GetIgnoredExceptions(Compilation compilation)
	{
		//
		// TODO: make this configurable
		//
		String[] names = new String[]
		{
			// Corrupted state exceptions - cannot be caught (unless manually thrown, which should be rare)
			typeof(OutOfMemoryException).FullName,
			typeof(StackOverflowException).FullName,

			// Asynchronous exceptions - largely deprecated
			typeof(ThreadAbortException).FullName,
			typeof(ThreadInterruptedException).FullName,

			// Programmer error
			typeof(OverflowException).FullName,
			typeof(DivideByZeroException).FullName,
			typeof(InvalidOperationException).FullName,
			typeof(NotImplementedException).FullName,
			typeof(ArgumentException).FullName,
		};

		foreach (String name in names)
		{
			ITypeSymbol? symbol = compilation.GetTypeByMetadataName(name);

			if (!(symbol is null))
			{
				yield return symbol;
			}
		}
	}
}
