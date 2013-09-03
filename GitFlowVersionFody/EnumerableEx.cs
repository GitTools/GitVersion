using System.Collections.Generic;
using System.Linq;

public static class EnumerableEx
{
	
	public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
	{
		return !enumerable.Any();
	}
}