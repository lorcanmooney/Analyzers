using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;

internal static class CompilationExtensions
{
	internal static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation @this, IEnumerable<String> fullyQualifiedMetadataNames)
	{
		foreach (String fullyQualifiedMetadataName in fullyQualifiedMetadataNames)
		{
			INamedTypeSymbol? type = @this.GetTypeByMetadataName(fullyQualifiedMetadataName);

			//
			// TODO: report name resolution failure instead of just skipping the type
			//
			if (!(type is null))
			{
				yield return type;
			}
		}
	}
}
