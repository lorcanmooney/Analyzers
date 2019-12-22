using System.Threading.Tasks;

namespace Analyzers.Tests
{
	internal static class Extensions
	{
		internal static T Await<T>(this Task<T> @this) => @this.GetAwaiter().GetResult();
	}
}
