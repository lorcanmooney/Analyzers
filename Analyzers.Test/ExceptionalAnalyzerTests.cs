using Microsoft.CodeAnalysis.Diagnostics;

using System;

using Xunit;

namespace Analyzers.Tests
{
	public class ExceptionalAnalyzerTests : AnalyzerTests
	{
		sealed protected override DiagnosticAnalyzer GetAnalyzer() => new ExceptionalAnalyzer();

		[Fact]
		public void Empty()
		{
			String source = @"";

			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionOnStaticConstructor()
		{
			String source = @"
using System;

class Foo
{
    /// [|<exception cref=""FormatException""></exception>|LM3005|]
    static Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void UncaughtExceptionInStaticConstructor()
		{
			String source = @"
using System;

class Foo
{
    static Foo()
    {
        [|throw|LM3005|] new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionThrownByInstanceConstructorBody()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""FormatException""></exception>
    Foo()
    {
        throw new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedDerivedExceptionThrownByInstanceConstructorBody()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""SystemException""></exception>
    Foo()
    {
        throw new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void UndocumentedExceptionThrownByInstanceConstructorBody()
		{
			String source = @"
using System;

class Foo
{
    Foo()
    {
        [|throw|LM3001:FormatException|] new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void UndocumentedExceptionThrownByImplicitBaseConstructor()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""ApplicationException""></exception>
    public Foo() { }
}

class Bar : Foo
{
    [|Bar|LM3002:ApplicationException|]()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionThrownByImplicitBaseConstructor()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""ApplicationException""></exception>
    public Foo() { }
}

class Bar : Foo
{
    /// <exception cref=""ApplicationException""></exception>
    Bar()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionThrownByInstanceMethodBody()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""FormatException""></exception>
    void Bar()
    {
        throw new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedDerivedExceptionThrownByInstanceMethodBody()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""SystemException""></exception>
    void Bar()
    {
        throw new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void UndocumentedExceptionThrownByInstanceMethodBody()
		{
			String source = @"
using System;

class Foo
{
    void Bar()
    {
        [|throw|LM3001:FormatException|] new FormatException();
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionThrownByInstanceMethodExpression()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""FormatException""></exception>
    void Bar() => throw new FormatException();
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedDerivedExceptionThrownByInstanceMethodExpression()
		{
			String source = @"
using System;

class Foo
{
    /// <exception cref=""Exception""></exception>
    void Bar() => throw new FormatException();
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void UndocumentedExceptionThrownByInstanceMethodExpression()
		{
			String source = @"
using System;

class Foo
{
    void Bar() => [|throw|LM3001|] new FormatException();
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionMatchesBaseMethodException()
		{
			String source = @"
using System;

abstract class Base
{
    /// <exception cref=""FormatException"" ></exception>
    protected abstract void Foo();
}

class Derived : Base
{
    /// <exception cref=""FormatException"" ></exception>
    protected override void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionDerivesFromBaseMethodException()
		{
			String source = @"
using System;

abstract class Base
{
    /// <exception cref=""Exception"" ></exception>
    protected abstract void Foo();
}

class Derived : Base
{
    /// <exception cref=""FormatException"" ></exception>
    protected override void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionIncompatibleWithBaseMethodExceptions()
		{
			String source = @"
using System;

abstract class Base
{
    /// <exception cref=""SystemException"" ></exception>
    protected abstract void Foo();
}

class Derived : Base
{
    /// <exception cref=""[|ApplicationException|LM3003:ApplicationException|]"" ></exception>
    protected override void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionMatchesInterfaceMethodException()
		{
			String source = @"
using System;

interface IInterface
{
    /// <exception cref=""FormatException"" ></exception>
    void Foo();
}

class Derived : IInterface
{
    /// <exception cref=""FormatException"" ></exception>
    public void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionDerivesFromInterfaceMethodException()
		{
			String source = @"
using System;

interface IInterface
{
    /// <exception cref=""Exception"" ></exception>
    void Foo();
}

class Derived : IInterface
{
    /// <exception cref=""FormatException"" ></exception>
    public void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}

		[Fact]
		public void DocumentedExceptionIncompatibleWithInterfaceMethodExceptions()
		{
			String source = @"
using System;

interface IInterface
{
    /// <exception cref=""SystemException"" ></exception>
    void Foo();
}

class Derived : IInterface
{
    /// <exception cref=""[|ApplicationException|LM3004:ApplicationException|]"" ></exception>
    public void Foo()
    {
    }
}
";
			VerifyDiagnostics(source);
		}
	}
}
