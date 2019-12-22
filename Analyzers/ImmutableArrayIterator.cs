using System;
using System.Collections.Immutable;

internal readonly struct ImmutableArrayIterator<T>
{
	public static ImmutableArrayIterator<T> Empty => new ImmutableArrayIterator<T>();

	private readonly ImmutableArray<T> _array;
	private readonly Int32 _index;

	public ImmutableArrayIterator(ImmutableArray<T> items) : this(items, 0)
	{
	}

	private ImmutableArrayIterator(ImmutableArray<T> items, Int32 index)
	{
		_array = items;
		_index = index;
	}

	public Boolean TryGetNext(out T head, out ImmutableArrayIterator<T> tail)
	{
		ImmutableArray<T> array = _array;
		Int32 index = _index;

		if (array.IsDefault || (index >= array.Length))
		{
			head = default!;
			tail = this;
			return false;
		}
		else
		{

			head = array[index];
			tail = new ImmutableArrayIterator<T>(array, (index + 1));
			return true;
		}
	}
}
