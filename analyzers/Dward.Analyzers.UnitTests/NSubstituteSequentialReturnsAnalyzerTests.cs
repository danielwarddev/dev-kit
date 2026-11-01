using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NSubstitute;
using Standards.Analyzers.UnitTests;

namespace Dward.Analyzers.UnitTests
{
    public class NSubstituteSequentialReturnsAnalyzerTests
    {
        private readonly CSharpAnalyzerTest<NSubstituteSequentialReturnsAnalyzer, DefaultVerifier> _analyzerTester;

        public NSubstituteSequentialReturnsAnalyzerTests()
        {
            _analyzerTester = new CSharpAnalyzerTest<NSubstituteSequentialReturnsAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = new ReferenceAssemblies("net10.0")
            };

            var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies");
            var referencePaths = Directory.GetFiles(referenceDirectory, "*.dll");
            if (referencePaths.Length == 0)
            {
                throw new InvalidOperationException($"Missing test reference assemblies in '{referenceDirectory}'.");
            }

            foreach (var referencePath in referencePaths)
            {
                var reference = MetadataReference.CreateFromFile(referencePath);
                _analyzerTester.TestState.AdditionalReferences.Add(reference);
            }

            var substituteReference = MetadataReference.CreateFromFile(typeof(Substitute).Assembly.Location);
            _analyzerTester.TestState.AdditionalReferences.Add(substituteReference);
            _analyzerTester.SolutionTransforms.Add((solution, projectId) =>
            {
                var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
                return solution.WithProjectParseOptions(projectId, parseOptions);
            });
        }

        [Theory]
        [InlineData("int[]")]
        [InlineData("System.Collections.Generic.List<int>")]
        [InlineData("System.Collections.Generic.IEnumerable<int>")]
        [InlineData("System.Collections.Generic.IReadOnlyList<int>")]
        public async Task When_Empty_Params_Array_Replaces_Second_Collection_Return_Then_Exact_Expression_Is_Reported(string returnType)
        {
            _analyzerTester.TestCode = $$"""
                                         using NSubstitute;

                                         public class Example
                                         {
                                             public void Configure({{returnType}} value)
                                             {
                                                 value.Returns(value, {|#0:[]|});
                                             }
                                         }
                                         """;

            _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                .WithLocation(0).WithArguments(returnType));

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Async_And_Static_Overloads_Use_Empty_Params_Then_Each_Is_Reported_Once()
        {
            _analyzerTester.TestCode = """
                                       using NSubstitute;
                                       using ReturnsApi = NSubstitute.SubstituteExtensions;
                                       using System.Threading.Tasks;

                                       public class Example
                                       {
                                           public void Configure(int[] first, Task<int[]> task, ValueTask<int[]> valueTask)
                                           {
                                               task.Returns(first, {|#0:[]|});
                                               valueTask.Returns(first, {|#1:[]|});
                                               ReturnsApi.Returns(first, returnThese: {|#2:[]|}, returnThis: first);
                                           }
                                       }
                                       """;

            for (var location = 0; location < 3; location++)
            {
                _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                    .WithLocation(location).WithArguments("int[]"));
            }

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Empty_Value_Is_Typed_Or_Expanded_Then_No_Diagnostic_Is_Reported()
        {
            _analyzerTester.TestCode = """
                                       using NSubstitute;
                                       using System.Threading.Tasks;

                                       public class Example
                                       {
                                           public void Configure(int[] first, Task<int[]> task, ValueTask<int[]> valueTask)
                                           {
                                               int[] empty = [];
                                               first.Returns([]);
                                               first.Returns(first, empty);
                                               first.Returns(first, (int[])[]);
                                               first.Returns(first, [], empty);
                                               first.Returns(first, [[]]);
                                               first.Returns(first);
                                               task.Returns(first, empty);
                                               valueTask.Returns(first, empty);
                                           }
                                       }
                                       """;

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Callback_Any_Args_Or_Scalar_Overload_Is_Used_Then_No_Diagnostic_Is_Reported()
        {
            _analyzerTester.TestCode = """
                                       using NSubstitute;
                                       using System.Threading.Tasks;

                                       public class Example
                                       {
                                           public void Configure(int[] first, Task<int[]> task, ValueTask<int[]> valueTask)
                                           {
                                               first.Returns(_ => first, []);
                                               task.Returns(_ => first, []);
                                               valueTask.Returns(_ => first, []);
                                               first.ReturnsForAnyArgs(first, []);
                                               1.Returns(1, []);
                                               "first".Returns("second", []);
                                           }
                                       }
                                       """;

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Unrelated_Returns_Method_Is_Called_Then_No_Diagnostic_Is_Reported()
        {
            _analyzerTester.TestCode = """
                                       public class Example
                                       {
                                           public void Returns(int[] first, params int[][] later) { }

                                           public void Configure(int[] first)
                                           {
                                               Returns(first, []);
                                           }
                                       }
                                       """;

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_NSubstitute_Is_Absent_Then_Same_Named_Api_Is_Ignored()
        {
            var substituteReference = _analyzerTester.TestState.AdditionalReferences.Last();
            _analyzerTester.TestState.AdditionalReferences.Remove(substituteReference);
            _analyzerTester.TestCode = """
                                       namespace NSubstitute
                                       {
                                           public static class SubstituteExtensions
                                           {
                                               public static void Returns<T>(this T value, T first, params T[] later) { }
                                           }
                                       }

                                       public class Example
                                       {
                                           public void Configure(int[] first)
                                           {
                                               NSubstitute.SubstituteExtensions.Returns(first, first, []);
                                           }
                                       }
                                       """;

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Returns_Is_In_Generated_Code_Then_No_Diagnostic_Is_Reported()
        {
            _analyzerTester.TestState.Sources.Add(("Example.g.cs", """
                                                                   // <auto-generated/>
                                                                   using NSubstitute;

                                                                   public class Example
                                                                   {
                                                                       public void Configure(int[] first)
                                                                       {
                                                                           first.Returns(first, []);
                                                                       }
                                                                   }
                                                                   """));

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task When_Rule_Is_Disabled_Then_No_Diagnostic_Is_Reported()
        {
            _analyzerTester.TestCode = """
                                       using NSubstitute;

                                       public class Example
                                       {
                                           public void Configure(int[] first)
                                           {
                                               first.Returns(first, []);
                                           }
                                       }
                                       """;
            _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
                is_global = true
                dotnet_diagnostic.DWARD0003.severity = none
                """));

            await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public void When_Bare_Empty_Collection_Is_Last_Then_First_Return_Repeats()
        {
            var values = Substitute.For<ISequentialValues>();
            int[] first = [1];
            values.Next().Returns(first, []);

            values.Next().Should().BeSameAs(first);
            values.Next().Should().BeSameAs(first);
        }

        [Fact]
        public async Task When_Async_Returns_Use_Bare_Empty_Params_Then_First_Value_Repeats()
        {
            var values = Substitute.For<ISequentialValues>();
            int[] first = [1];
            values.NextTask().Returns(first, []);
            values.NextValueTask().Returns(first, []);

            var firstTaskValue = await values.NextTask();
            var secondTaskValue = await values.NextTask();
            firstTaskValue.Should().BeSameAs(first);
            secondTaskValue.Should().BeSameAs(first);
            var firstValueTaskValue = await values.NextValueTask();
            var secondValueTaskValue = await values.NextValueTask();
            firstValueTaskValue.Should().BeSameAs(first);
            secondValueTaskValue.Should().BeSameAs(first);
        }

        [Fact]
        public async Task When_Typed_Empty_Value_Is_Last_Then_Sequential_Returns_Include_Empty_Value()
        {
            var values = Substitute.For<ISequentialValues>();
            int[] first = [1];
            int[] empty = [];
            values.Next().Returns(first, empty);
            values.NextTask().Returns(first, empty);
            values.NextValueTask().Returns(first, empty);

            values.Next().Should().BeSameAs(first);
            values.Next().Should().BeSameAs(empty);
            values.Next().Should().BeSameAs(empty);
            var firstTaskValue = await values.NextTask();
            var secondTaskValue = await values.NextTask();
            firstTaskValue.Should().BeSameAs(first);
            secondTaskValue.Should().BeSameAs(empty);
            var firstValueTaskValue = await values.NextValueTask();
            var secondValueTaskValue = await values.NextValueTask();
            firstValueTaskValue.Should().BeSameAs(first);
            secondValueTaskValue.Should().BeSameAs(empty);
        }
    }
}

namespace Standards.Analyzers.UnitTests
{
    public interface ISequentialValues
    {
        int[] Next();
        Task<int[]> NextTask();
        ValueTask<int[]> NextValueTask();
    }
}
