using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

internal readonly struct ExceptionalOperation
{
	internal readonly Location Location;
	internal readonly IMethodSymbol? Method;
	internal readonly ITypeSymbol Type;

	//
	// HACK: Implicit/compiler-generated calls to parameterless base-class constructors are
	//       not included in the operation hierarchy and need to be handled explicitly.
	//
	public ExceptionalOperation(Location location, IMethodSymbol method, ITypeSymbol type)
	{
		Location = location;
		Method = method;
		Type = type;
	}

	public ExceptionalOperation(IObjectCreationOperation operation, ITypeSymbol type)
	{
		Location = operation.GetExceptionalLocation();
		Method = operation.Constructor;
		Type = type;
	}

	public ExceptionalOperation(IInvocationOperation operation, ITypeSymbol type)
	{
		Location = operation.GetExceptionalLocation();
		Method = operation.TargetMethod;
		Type = type;
	}

	public ExceptionalOperation(IThrowOperation operation)
	{
		Location = operation.GetExceptionalLocation();
		Method = null;
		Type = operation.Exception.GetUnconvertedType();
	}
}
