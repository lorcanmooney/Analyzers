using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Xunit;

namespace Analyzers.Tests
{
	public abstract class AnalyzerTests
	{
		private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(Object).Assembly.Location);
		private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
		private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
		private static readonly MetadataReference CSharpCodeAnalysisReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);

		private static Project CreateProject(params String[] sources)
		{
			Project project = CreateProject();

			const String fileNamePrefix = "TestFile";
			const String fileNameExtension = "cs";

			for (Int32 i = 0; i < sources.Length; i++)
			{
				String fileName = (fileNamePrefix + i + "." + fileNameExtension);
				project = project.AddDocument(fileName, SourceText.From(sources[i])).Project;
			}

			return project;
		}

		private static Project CreateProject()
		{
			const String projectName = "TestProject";

			Project project = new AdhocWorkspace()
				.CurrentSolution
				.AddProject(projectName, projectName, LanguageNames.CSharp)
				.AddMetadataReference(CorlibReference)
				.AddMetadataReference(SystemCoreReference)
				.AddMetadataReference(CSharpCodeAnalysisReference)
				.AddMetadataReference(CodeAnalysisReference);

			CompilationOptions options = project.CompilationOptions
				.WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

			return project.WithCompilationOptions(options);
		}

		private protected void VerifyDiagnostics(String source)
		{
			ExpectedDiagnostic[] expectedDiagnostics = GetExpectedDiagnostics(ref source);

			Compilation compilation = CreateProject(source).GetCompilationAsync().Await();

			//
			// TODO:
			//
			SyntaxTree tree = compilation.SyntaxTrees.Single();

			Boolean compilationHasErrors = compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

			Assert.False(compilationHasErrors, "Supplied source code has errors.");

			ImmutableArray<Diagnostic> actualDiagnostics = compilation
				.WithAnalyzers(ImmutableArray.Create(GetAnalyzer()))
				.GetAnalyzerDiagnosticsAsync()
				.Await();

			Assert.Equal(expectedDiagnostics.Length, actualDiagnostics.Length);

			for (Int32 i = 0; i < actualDiagnostics.Length; i++)
			{
				Diagnostic actualDiagnostic = actualDiagnostics[i];
				ExpectedDiagnostic expectedDiagnostic = expectedDiagnostics[i];

				Assert.Equal(expectedDiagnostic.Id, actualDiagnostic.Id);

				String actualMessage = actualDiagnostic.GetMessage(CultureInfo.InvariantCulture);

				String[] expectedMessageParts = expectedDiagnostic.MessageParts;

				if (!(expectedMessageParts is null))
				{
					foreach (String expectedMessagePart in expectedMessageParts)
					{
						Assert.True(actualMessage.Contains(expectedMessagePart), $"Missing expected message content '{expectedMessagePart}'.");
					}
				}

				FileLinePositionSpan actualSpan = actualDiagnostic.Location.GetLineSpan();
				FileLinePositionSpan expectedSpan = tree.GetMappedLineSpan(expectedDiagnostic.Span);

				Assert.Equal(expectedSpan, actualSpan);
			}
		}

		private ExpectedDiagnostic[] GetExpectedDiagnostics(ref String source)
		{
			List<ExpectedDiagnostic> expectedDiagnostics = new List<ExpectedDiagnostic>();

			Int32 start;

			while ((start = source.IndexOf("[|")) >= 0)
			{
				source = source.Remove(start, 2);

				Int32 end = source.IndexOf('|', start);

				Int32 expectedStart = start;
				Int32 expectedEnd = end;

				source = source.Remove(end, 1);

				start = end;
				end = source.IndexOf("|]", start);

				expectedDiagnostics.Add(
					GetExpectedDiagnostic(
						new TextSpan(expectedStart, (expectedEnd - expectedStart)),
						source.Substring(start, (end - start))
					)
				);

				source = source.Remove(start, ((end - start) + 2));
			}

			return expectedDiagnostics.ToArray();
		}
		
		private ExpectedDiagnostic GetExpectedDiagnostic(TextSpan span, String text)
		{
			String id;
			String[] messageParts;

			Int32 index = text.IndexOf(':');

			if (index < 0)
			{
				id = text;
				messageParts = Array.Empty<String>();
			}
			else
			{
				id = text.Substring(0, index);
				messageParts = text.Substring(index + 1).Split(';');
			}

			return new ExpectedDiagnostic
			{
				Id = id,
				Span = span,
				MessageParts = messageParts,
			};
		}

		protected abstract DiagnosticAnalyzer GetAnalyzer();
	}
}
