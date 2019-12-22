using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

internal struct TypeSet
{
	public static TypeSet Empty => new TypeSet(ImmutableArray<ITypeSymbol>.Empty);
	
	public static TypeSet Universal => new TypeSet(default);

	public static TypeSet operator &(TypeSet a, TypeSet b) => Intersect(a._types, b._types);

	private static TypeSet Intersect(ImmutableArray<ITypeSymbol> a, ImmutableArray<ITypeSymbol> b)
	{
		if (a.IsDefault)
		{
			return new TypeSet(b);
		}
		else if (b.IsDefault)
		{
			return new TypeSet(a);
		}
		else
		{
			TypeSet intersection = Empty;

			//
			// TODO: this seem ... inefficient
			//
			for (Int32 i = 0; i < a.Length; i++)
			{
				for (Int32 j = 0; j < b.Length; j++)
				{
					if (a[i].IsAssignableTo(b[j]))
					{
						intersection.Add(b);
					}
					else if (b[j].IsAssignableTo(a[i]))
					{
						intersection.Add(a);
					}
				}
			}

			return intersection;
		}
	}

	private readonly ImmutableArray<ITypeSymbol> _types;

	private TypeSet(ImmutableArray<ITypeSymbol> types)
	{
		_types = types;
	}

	internal TypeSet Add(IEnumerable<ITypeSymbol> types)
	{
		TypeSet union = this;

		foreach (ITypeSymbol type in types)
		{
			union = union.Add(type);
		}

		return union;
	}

	internal TypeSet Add(ITypeSymbol type)
	{
		ImmutableArray<ITypeSymbol> types = _types;

		if (types.IsDefault)
		{
			// This is the universal set
			return this;
		}
		else
		{
			// The check if the union already covers the new type
			for (Int32 i = 0; i < types.Length; i++)
			{
				if (type.IsAssignableTo(types[i]))
				{
					return this;
				}
			}

			// The new type may be a super-type of some types already in the union
			for (Int32 i = 0; i < types.Length; i++)
			{
				if (types[i].IsAssignableTo(type))
				{
					types = types.RemoveAt(i);
					i--;
				}
			}

			return new TypeSet(types.Add(type));
		}
	}

	internal Boolean Contains(ITypeSymbol type)
	{
		ImmutableArray<ITypeSymbol> types = _types;

		if (types.IsDefault)
		{
			return true;
		}
		else
		{
			for (Int32 i = 0; i < types.Length; i++)
			{
				if (type.IsAssignableTo(types[i]))
				{
					return true;
				}
			}
		}

		return false;
	}
}
