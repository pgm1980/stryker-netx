using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Core.Mutants.CsharpNodeOrchestrators;
using Stryker.Core.Tests.Integration;
using Stryker.Utilities.Logging;
using Xunit;

namespace Stryker.Core.Tests.Mutants;

/// <summary>
/// Sprint 160 (ADR-040): unit tests for <see cref="CommentParser"/> covering the
/// three backwards-compatible sub-fixes:
/// <list type="bullet">
///   <item>D-γ: skip-on-fail label parsing (no silent <c>Mutator.Statement</c> fallback)</item>
///   <item>D-β: <c>next-line</c> scope-qualifier accepted as alias for <c>once</c></item>
///   <item>D-α: PascalCase class-name hint on unrecognised labels</item>
/// </list>
/// Tests exercise the public <see cref="CommentParser.ParseNodeLeadingComments"/> API
/// and inspect the internal <see cref="MutationContext.FilteredMutators"/> property
/// (accessible via <c>InternalsVisibleTo("Stryker.Core.Tests")</c> in Stryker.Core.csproj).
/// </summary>
public sealed class CommentParserTests : IntegrationTestBase
{
    static CommentParserTests()
    {
        ApplicationLogging.LoggerFactory ??= NullLoggerFactory.Instance;
    }

    private static MutationContext BuildContext() =>
        new(BuildOrchestrator());

    private static ExpressionStatementSyntax NodeWithLeadingComment(string comment)
    {
        var source = $"class C {{ void M() {{ {comment}\n x++; }} }}";
        var tree = CSharpSyntaxTree.ParseText(source);
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .First();
    }

    /// <summary>
    /// Block-disable with mutator-keyword "all" must populate
    /// <see cref="MutationContext.FilteredMutators"/> with every value of the
    /// <see cref="Mutator"/> enum.
    /// </summary>
    [Fact]
    public void Disable_Block_All_AppliesAllKinds()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable all : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(
            Enum.GetValues<Mutator>(),
            "block-disable all must filter every Mutator value");
    }

    /// <summary>
    /// Block-disable with label "Boolean" must set <c>FilteredMutators</c> to
    /// exactly <c>[Mutator.Boolean]</c> and preserve the comment text.
    /// </summary>
    [Fact]
    public void Disable_Block_Boolean_AppliesBooleanOnly()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable Boolean : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "block-disable Boolean must filter only Boolean");
        result.FilterComment.Should().Be("reason",
            "the comment text must be preserved exactly");
    }

    /// <summary>
    /// The <c>once</c> scope qualifier must create a new context (different
    /// reference) so that the filter applies only to a single mutation site.
    /// </summary>
    [Fact]
    public void Disable_Once_Boolean_NewContext()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable once Boolean : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.Should().NotBeSameAs(ctx,
            "once-scope must create a fresh context, not mutate the parent");
        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean);
    }

    /// <summary>
    /// ADR-040 D-β verification: <c>next-line</c> must be accepted without ERR-log
    /// and behave like <c>once</c> (single-mutation scope, new context, full set for "all").
    /// </summary>
    [Fact]
    public void Disable_NextLine_All_NewContext()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line all : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.Should().NotBeSameAs(ctx,
            "next-line scope must create a fresh context, identical to once-behaviour");
        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(
            Enum.GetValues<Mutator>(),
            "next-line all must filter every Mutator value");
    }

    /// <summary>
    /// <c>next-line</c> with a specific mutator label must apply a single-kind filter
    /// in a new context.
    /// </summary>
    [Fact]
    public void Disable_NextLine_Boolean_NewContext()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line Boolean : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.Should().NotBeSameAs(ctx,
            "next-line scope must create a fresh context");
        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean);
    }

    /// <summary>
    /// ADR-040 D-α verification: "ConfigureAwait" is a Mutator class name, not a
    /// Mutator-Kind name.  The parser must skip it (empty <c>FilteredMutators</c>),
    /// not silently apply a <c>Mutator.Statement</c> fallback.
    /// </summary>
    [Fact]
    public void Disable_NextLine_ClassName_SkipsLabelWithHint()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line ConfigureAwait : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull(
            "FilterMutators is still called, but with an empty array");
        result.FilteredMutators!.Should().BeEmpty(
            "unrecognised label 'ConfigureAwait' must be skipped — no silent Mutator.Statement fallback");
    }

    /// <summary>
    /// ADR-040 D-γ verification (mixed input): when a comma-separated label list
    /// contains both a valid Mutator-Kind ("Boolean") and an invalid one
    /// ("ConfigureAwait"), only the valid one must appear in <c>FilteredMutators</c>.
    /// Specifically, <c>Mutator.Statement</c> must NOT appear.
    /// </summary>
    [Fact]
    public void Disable_Mixed_Valid_And_Invalid_PartialApply()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable Boolean, ConfigureAwait : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "only the valid label Boolean must be in the filter");
        result.FilteredMutators.Should().NotContain(Mutator.Statement,
            "Mutator.Statement must NOT appear — that would indicate the pre-ADR-040 fallback bug");
    }

    /// <summary>
    /// ADR-040 D-γ critical-path verification: a fully unrecognised label ("garbage")
    /// must produce an EMPTY <c>FilteredMutators</c> array.  The pre-fix code filled the
    /// array with <c>default(Mutator)</c> which equals <c>Mutator.Statement</c>, silently
    /// disabling statement mutations.
    /// </summary>
    [Fact]
    public void Disable_Block_InvalidLabel_NoStatementFallback()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable garbage : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEmpty(
            "a fully unrecognised label must be skipped — empty, not [Mutator.Statement]");
        result.FilteredMutators.Should().NotContain(Mutator.Statement,
            "Mutator.Statement appearing here would indicate the pre-ADR-040 silent-fallback bug");
    }

    /// <summary>
    /// A restore-all comment on a context that already has a filter must remove the
    /// matching entries.  <see cref="MutationContext.FilterMutators"/> with
    /// <c>mode=false</c> removes the listed mutators from <c>FilteredMutators</c>.
    /// </summary>
    [Fact]
    public void Restore_All_Mode()
    {
        var ctx = BuildContext();
        var disableNode = NodeWithLeadingComment("// Stryker disable Boolean");
        var disabledCtx = CommentParser.ParseNodeLeadingComments(disableNode, ctx);
        disabledCtx.FilteredMutators.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean, "pre-condition: Boolean is disabled");

        var restoreNode = NodeWithLeadingComment("// Stryker restore all");
        var result = CommentParser.ParseNodeLeadingComments(restoreNode, disabledCtx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().NotContain(Mutator.Boolean,
            "restore all must remove all previously disabled mutators");
    }

    /// <summary>
    /// When no colon separator is present in the Stryker comment, the parser must use
    /// the built-in default comment text rather than an empty string.
    /// </summary>
    [Fact]
    public void CommentNoColon_DefaultsComment()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable Boolean");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilterComment.Should().Be(
            "Ignored via code comment.",
            "missing colon plus comment text must fall back to the built-in default");
    }

    /// <summary>
    /// Mutator label parsing is case-insensitive. Both "BOOLEAN" and "boolean" must
    /// resolve to <c>Mutator.Boolean</c>.
    /// </summary>
    [Fact]
    public void CaseInsensitive_Mutator()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable BOOLEAN : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "mutator label parsing must be case-insensitive");
    }
}
