using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 51 (v2.38.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class BlockMutatorTests
{
    [Fact]
    public void ShouldMutateNonEmptyConstructorOnClass()
    {
        const string Source = @"
class Program
{
    int doesNotNeedToBeInitialized;

    Program()
    {
        this.doesNotNeedToBeInitialized = 42;
    }
}";

        var mutations = GetMutations(Source).ToList();
        mutations.Should().ContainSingle();
        mutations[0].ReplacementNode.ChildNodes().Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateStructConstructorAssignments()
    {
        const string Source = @"
struct Program
{
    int mustBeInitialized;
    bool alsoThisMustBeInitialized;

    Program(int value)
    {
        this.mustBeInitialized = value;

        if (value == 0)
        {
            this.alsoThisMustBeInitialized = true;
        }
        else
        {
            this.alsoThisMustBeInitialized = false;
        }
    }
}";

        GetMutations(Source).Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateSwitchCasesBlock()
    {
        const string Source = @"
struct Program
{
    int mustBeInitialized;
    bool alsoThisMustBeInitialized;

    Program(int value)
    {
        switch(value)
        {
            default:
            {
                value++;
            }
        }
    }
}";

        GetMutations(Source).Should().ContainSingle();
    }

    [Fact]
    public void ShouldMutateLocalFunctionsInStructConstructors()
    {
        const string Source = @"
struct Program
{
    int mustBeInitialized;

    Program(int value)
    {
        int CalculateValue()
        {
            int value;
            value = 42;
            return value;
        }

        this.mustBeInitialized = CalculateValue();
    }
}";

        GetMutations(Source).Count().Should().Be(1, "Should mutate the local function and only the local function");
    }

    [Fact]
    public void ShouldMutateStructConstructorNonAssignmentsAtRoot()
    {
        const string Source = @"
struct Program
{
    Program(int value)
    {
        throw new Exception();
    }
}";

        GetMutations(Source).Count().Should().Be(1);
    }

    [Fact]
    public void ShouldMutateStructConstructorNonAssignmentChild()
    {
        const string Source = @"
struct Program
{
    int mustBeInitialized;

    Program(int value)
    {
        if (value == 0)
        {
            throw new Exception();
        }

        this.mustBeInitialized = value;
    }
}";

        GetMutations(Source).Count().Should().Be(1);
    }

    [Fact]
    public void ShouldMutateVoidReturnsAsEmptyInMethod()
    {
        const string Source = @"
class Program
{
    void Method(bool input)
    {
        if (input)
        {
            return;
        }
    }
}";

        GetMutations(Source).Should().AllSatisfy(mutation => mutation.ReplacementNode.ChildNodes().Should().BeEmpty());
    }

    [Fact]
    public void ShouldMutateVoidReturnsAsEmptyInLocalFunction()
    {
        const string Source = @"
class Program
{
    int Method(bool input)
    {
        void LocalFunction()
        {
            return;
        }

        return 42;
    }
}";

        var mutations = GetMutations(Source)
            .Where(mutation => mutation.OriginalNode.Parent is LocalFunctionStatementSyntax { Identifier.Text: "LocalFunction" })
            .ToList();
        mutations.Should().ContainSingle();
        mutations[0].ReplacementNode.ChildNodes().Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateAlreadyEmptyBlocks()
    {
        const string Source = @"
class Program
{
    private Program()
    {
        // Nothing to do
    }

    void Method()
    {
        // Nothing to do
    }
}";
        GetMutations(Source).Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateInfiniteWhileLoops()
    {
        const string Source = @"
class Program
{
    void Method()
    {
        while (true)
        {
            break;
        }
    }
}";
        GetMutations(Source)
            .Where(mutation => mutation.OriginalNode.Parent is WhileStatementSyntax)
            .Should().BeEmpty();
    }

    private static IEnumerable<Mutation> GetMutations(string source)
    {
        var statements = CSharpSyntaxTree
            .ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<BlockSyntax>();

        var target = new BlockMutator();
        return statements.SelectMany(statement => target.ApplyMutations(statement, null!));
    }
}
