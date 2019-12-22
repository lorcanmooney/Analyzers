using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

internal static class OperationExtensions
{
	internal static Location GetExceptionalLocation(this IInvocationOperation @this)
	{
		if (@this.Syntax is InvocationExpressionSyntax invocationSyntax)
		{
			if (invocationSyntax.Expression is SimpleNameSyntax simpleNameSyntax)
			{
				return simpleNameSyntax.GetLocation();
			}
			else if (invocationSyntax.Expression is QualifiedNameSyntax qualifiedNameSyntax)
			{
				return qualifiedNameSyntax.Right.GetLocation();
			}
			else if (invocationSyntax.Expression is MemberAccessExpressionSyntax memberSyntax)
			{
				return memberSyntax.Name.GetLocation();
			}
			else
			{
				return @this.Syntax.GetLocation();
			}
		}
		else if (@this.Syntax is ConstructorInitializerSyntax initializerSyntax)
		{
			return initializerSyntax.ThisOrBaseKeyword.GetLocation();
		}
		else
		{
			return @this.Syntax.GetLocation();
		}
	}

	internal static Location GetExceptionalLocation(this IObjectCreationOperation @this)
	{
		if (@this.Syntax is ObjectCreationExpressionSyntax creationSyntax)
		{
			return creationSyntax.Type.GetLocation();
		}
		else
		{
			return @this.Syntax.GetLocation();
		}
	}

	internal static Location GetExceptionalLocation(this IThrowOperation @this)
	{
		if (@this.Syntax is ThrowExpressionSyntax expressionSyntax)
		{
			return expressionSyntax.ThrowKeyword.GetLocation();
		}
		else if (@this.Syntax is ThrowStatementSyntax statementSyntax)
		{
			return statementSyntax.ThrowKeyword.GetLocation();
		}
		else
		{
			return @this.Syntax.GetLocation();
		}
	}

	internal static ImmutableArray<ISymbol> GetInitializedMembers(this ISymbolInitializerOperation @this)
	{
		if (@this is IFieldInitializerOperation fieldInitializer)
		{
			return fieldInitializer.InitializedFields.CastArray<ISymbol>();
		}
		else if (@this is IPropertyInitializerOperation propertyInitializer)
		{
			return propertyInitializer.InitializedProperties.CastArray<ISymbol>();
		}
		else
		{
			// Not a member initializer
			return ImmutableArray<ISymbol>.Empty;
		}
	}

	internal static IEnumerable<IMethodSymbol> GetInitializationConstructors(this ISymbolInitializerOperation @this)
	{
		INamedTypeSymbol? type = null;
		Boolean isStatic = false;

		foreach (ISymbol symbol in @this.GetInitializedMembers())
		{
			type = symbol.ContainingType;
			isStatic = symbol.IsStatic;

			if (!(type is null))
			{
				break;
			}
		}

		if (type is null)
		{
			return Enumerable.Empty<IMethodSymbol>();
		}
		else
		{
			return isStatic ? type.StaticConstructors : type.GetInstanceInitializationConstructors(); 
		}
	}

	internal static ITypeSymbol GetUnconvertedType(this IOperation @this)
	{
		if ((@this is IConversionOperation conversion) && conversion.IsImplicit)
		{
			return conversion.Operand.Type;
		}
		else
		{
			return @this.Type;
		}
	}

	internal static IMethodSymbol GetSymbol(this IMethodBodyBaseOperation @this)
	{
		return ((IMethodSymbol)(@this.SemanticModel.GetDeclaredSymbol(@this.Syntax)));
	}

	internal static MemberDeclarationSyntax GetDeclarationSyntax(this IMethodBodyBaseOperation @this)
	{
		return @this.Syntax.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
	}

	internal static IEnumerable<ITypeSymbol> GetDocumentedExceptions(this IMethodBodyBaseOperation @this)
	{
		MemberDeclarationSyntax declaration = @this.GetDeclarationSyntax();

		foreach (CrefSyntax cref in declaration.GetDocumentedExceptions())
		{
			if (@this.SemanticModel.GetSymbolInfo(cref).Symbol is ITypeSymbol exception)
			{
				yield return exception;
			}
		}
	}

	internal static IEnumerable<ExceptionalOperation> GetExceptionalOperations(this IOperation @this, Compilation compilation)
	{
		return @this.GetExceptionalOperations(compilation, TypeSet.Empty);
	}

	private static IEnumerable<ExceptionalOperation> GetExceptionalOperations(this IOperation @this, Compilation compilation, TypeSet handled)
	{
		return @this.Children.GetExceptionalOperations(compilation, handled);
	}

	private static IEnumerable<ExceptionalOperation> GetExceptionalOperations(this IEnumerable<IOperation> @this, Compilation compilation, TypeSet handled)
	{
		IEnumerable<IOperation> head = @this;
		Stack<IEnumerable<IOperation>> tail = new Stack<IEnumerable<IOperation>>();

		do
		{
			foreach (IOperation operation in head)
			{
				if (operation is IThrowOperation throwOperation)
				{
					yield return new ExceptionalOperation(throwOperation);

					tail.Push(throwOperation.Children);
				}
				else if (operation is IInvocationOperation invocationOperation)
				{
					IMethodSymbol callee = invocationOperation.TargetMethod;

					foreach (ITypeSymbol exception in callee.GetDocumentedExceptions(compilation))
					{
						yield return new ExceptionalOperation(invocationOperation, exception);
					}

					tail.Push(invocationOperation.Children);
				}
				else if (operation is IObjectCreationOperation creationOperation)
				{
					IMethodSymbol constructor = creationOperation.Constructor;

					foreach (ITypeSymbol exception in constructor.GetDocumentedExceptions(compilation))
					{
						yield return new ExceptionalOperation(creationOperation, exception);
					}

					tail.Push(creationOperation.Children);
				}
				else if (operation is ITryOperation tryOperation)
				{
					tail.Push(tryOperation.Finally.Children);

					TypeSet caught = handled;

					foreach (ICatchClauseOperation catchOperation in tryOperation.Catches)
					{
						tail.Push(catchOperation.Handler.Children);

						IOperation? filterOperation = catchOperation.Filter;

						if (filterOperation is null)
						{
							caught = caught.Add(catchOperation.ExceptionType);
						}
						else
						{
							tail.Push(filterOperation);
						}
					}

					foreach (ExceptionalOperation unhandledException in tryOperation.Body.GetExceptionalOperations(compilation, caught))
					{
						yield return unhandledException;
					}
				}
				else
				{
					tail.Push(operation.Children);
				}
			}
		}
		while (tail.TryPop(out head));
	}
}
