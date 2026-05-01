using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class UoiMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<UoiMutator>(MutationProfile.All);

    [Fact]
    public void Type_IsUoiMutator()
        => typeof(UoiMutator).Should().NotBeNull();

    [Fact]
    public void DoesNotMutate_IdentifierInsideQualifiedName()
    {
        // Sprint 23 regression: a namespace declaration's qualified name has
        // two identifier children. UOI used to fire on those children; the
        // conditional placer then produced a parenthesised expression in the
        // name-syntax slot, which Roslyn's qualified-name visitor cannot accept.
        // Reproducer was the Complete level combined with the All profile.
        var tree = CSharpSyntaxTree.ParseText("namespace Sample.Library; class C {}");
        var qualifiedName = tree.GetRoot().DescendantNodes().OfType<QualifiedNameSyntax>().Single();
        var library = (IdentifierNameSyntax)qualifiedName.Right;
        var sample = (IdentifierNameSyntax)qualifiedName.Left;

        var mutator = new UoiMutator();
        ApplyMutations(mutator, library).Should().BeEmpty(
            "UOI must skip NameSyntax-typed slots (right-hand of QualifiedName)");
        ApplyMutations(mutator, sample).Should().BeEmpty(
            "UOI must skip NameSyntax-typed slots (left-hand of QualifiedName)");
    }

    [Fact]
    public void DoesNotMutate_IdentifierInsideAliasQualifiedName()
    {
        // global::Foo — `Foo` lives in an AliasQualifiedNameSyntax slot that
        // also requires NameSyntax. Same crash class as QualifiedName.
        var tree = CSharpSyntaxTree.ParseText("class C { global::System.String s; }");
        var aliasQualified = tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().FirstOrDefault();
        aliasQualified.Should().NotBeNull("test setup: source must contain an alias-qualified name");
        var nameUnderAlias = aliasQualified!.Name;
        if (nameUnderAlias is IdentifierNameSyntax id)
        {
            ApplyMutations(new UoiMutator(), id).Should().BeEmpty(
                "UOI must skip the right-hand of an AliasQualifiedName");
        }
    }
}
