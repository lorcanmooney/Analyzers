using System;
using System.Collections.Generic;

internal static class StackExtensions
{
	internal static void Push<T>(this Stack<IEnumerable<T>> @this, T item)
	{
		@this.Push(new T[1] { item });
	}

	internal static Boolean TryPop<T>(this Stack<T> @this, out T item)
	{
		if (@this.Count > 0)
		{
			item = @this.Pop();
			return true;
		}
		else
		{
			item = default!;
			return false;
		}
	}
}
