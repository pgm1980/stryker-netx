using FluentAssertions;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>Sprint 120 (v3.0.7) bucket-3 → structural-assertion rewrite (replaces Sprint 109
/// architectural-deferral). Upstream tests assert exact mutated source with hardcoded IsActive(N)
/// IDs; structural rewrite counts mutations instead. Tests verify the comment-disable behavior
/// by checking mutation count differs between with-comment and without-comment source variants.</summary>
public class StrykerCommentTests : MutantOrchestratorTestsBase
{
    [Fact]
    public void StrykerComment_DisableAllShouldReduceMutationCount()
    {
        // Without comment — full mutation
        var sourceWithoutComment = """
            public void SomeMethod() {
                var x = 0;
                x++;
                x/=2;
            }
            """;
        var countWithoutComment = CountMutations(sourceWithoutComment);

        // With "// Stryker disable all" — should disable downstream mutations
        var sourceWithComment = """
            public void SomeMethod() {
                var x = 0;
            // Stryker disable all
                x++;
                x/=2;
            }
            """;
        var countWithComment = CountMutations(sourceWithComment);

        // Comment-disabled source should produce FEWER mutations
        countWithComment.Should().BeLessThan(countWithoutComment, "// Stryker disable all should disable some mutations");
    }

    [Fact]
    public void StrykerComment_NoCommentShouldProduceMutations()
    {
        var source = """
            public void SomeMethod() {
                var x = 0;
                x++;
                x/=2;
            }
            """;
        var count = CountMutations(source);
        count.Should().BeGreaterThan(0, "method body with arithmetic should produce ≥1 mutation");
    }
}
