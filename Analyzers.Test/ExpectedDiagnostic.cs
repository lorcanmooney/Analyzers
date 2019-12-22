using Microsoft.CodeAnalysis.Text;

using System;

namespace Analyzers.Tests
{
	internal struct ExpectedDiagnostic
	{
		public String Id;
		public TextSpan Span;
		public String[] MessageParts;
	}
}
