using System.Collections.Generic;
using System.Linq;

internal static class EnumerableExtensions
{
	internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> @this) where T : class
	{
		return @this.Where(t => !(t is null))!;
	}
}
