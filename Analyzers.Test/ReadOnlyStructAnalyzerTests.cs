using Microsoft.CodeAnalysis.Diagnostics;

using System;

using Xunit;

namespace Analyzers.Tests
{
	public class ReadOnlyStructAnalyzerTests : AnalyzerTests
	{
		sealed protected override DiagnosticAnalyzer GetAnalyzer() => new ReadOnlyStructAnalyzer();

		[Fact]
		public void Empty()
		{
			String source = @"";

			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableFieldStructWithReadOnlyModifier()
		{
			String source = @"
readonly struct Foo
{
    readonly int i;
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutablePropertyStructWithReadOnlyModifier()
		{
			String source = @"
readonly struct Foo
{
    int Bar { get; }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableEmptyStructWithoutReadOnlyModifier()
		{
			String source = @"
struct [|Foo|LM2001|]
{
    readonly int i;
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableFieldStructWithoutReadOnlyModifier()
		{
			String source = @"
struct [|Foo|LM2001|]
{
    readonly int i;
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableAutoPropertyStructWithoutReadOnlyModifier()
		{
			String source = @"
struct [|Foo|LM2001|]
{
    int Bar { get; }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutablePropertyStructWithoutReadOnlyModifier()
		{
			String source = @"
using System;

struct [|Foo|LM2001|]
{
    int Bar
    {
        get { throw new Exception(); }
        set { throw new Exception(); }
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableEventStructWithoutReadOnlyModifier()
		{
			String source = @"
using System;

struct [|Foo|LM2001|]
{
    event Action Bar
    {
        add { throw new Exception(); }
        remove { throw new Exception(); }
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void ImmutableEventStructWithReadOnlyModifier()
		{
			String source = @"
using System;

readonly struct Foo
{
    event Action Bar
    {
        add { throw new Exception(); }
        remove { throw new Exception(); }
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void MutableFieldStruct()
		{
			String source = @"
struct Foo
{
    int i;
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void MutableAutoPropertyStruct()
		{
			String source = @"
struct Foo
{
    int Bar { get; set; }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void MutableEventStruct()
		{
			String source = @"
using System;

struct Foo
{
    event Action Bar;
}
";
			VerifyDiagnostics(source);
		}
	}
}
