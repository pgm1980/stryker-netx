using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
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
    /// ADR-040 D-α verification: a PascalCase class name NOT in the Sprint-166
    /// alias table (e.g., <c>NakedReceiver</c>) is a Mutator class name, not a
    /// Mutator-Kind name. The parser must skip it (empty <c>FilteredMutators</c>),
    /// not silently apply a <c>Mutator.Statement</c> fallback.
    /// </summary>
    /// <remarks>
    /// Sprint 166 (ADR-046 §B) added <c>ConfigureAwait</c> / <c>AsyncAwait</c> /
    /// <c>AsyncAwaitResult</c> as RECOGNIZED aliases mapping to
    /// <see cref="Mutator.Boolean"/>. This test uses <c>NakedReceiver</c> (a real
    /// mutator class name NOT in the alias table) to keep validating the
    /// skip-on-unrecognised behaviour.
    /// </remarks>
    [Fact]
    public void Disable_NextLine_ClassName_SkipsLabelWithHint()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line NakedReceiver : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull(
            "FilterMutators is still called, but with an empty array");
        result.FilteredMutators!.Should().BeEmpty(
            "unrecognised label 'NakedReceiver' must be skipped — no silent Mutator.Statement fallback");
    }

    /// <summary>
    /// ADR-040 D-γ verification (mixed input): when a comma-separated label list
    /// contains both a valid Mutator-Kind ("Boolean") and an invalid one
    /// (<c>NakedReceiver</c>, NOT in Sprint-166 alias table), only the valid one
    /// must appear in <c>FilteredMutators</c>. Specifically, <c>Mutator.Statement</c>
    /// must NOT appear.
    /// </summary>
    [Fact]
    public void Disable_Mixed_Valid_And_Invalid_PartialApply()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable Boolean, NakedReceiver : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "only the valid label Boolean must be in the filter");
        result.FilteredMutators.Should().NotContain(Mutator.Statement,
            "Mutator.Statement must NOT appear — that would indicate the pre-ADR-040 fallback bug");
    }

    /// <summary>
    /// Sprint 166 (ADR-046 §B, Aisess §7 + Wishlist #6): the <c>ConfigureAwait</c>
    /// class name is now a RECOGNIZED alias for <see cref="Mutator.Boolean"/>.
    /// Previously (pre-Sprint-166) this label produced an ERR-log via the Sprint-161
    /// hint mechanism — now it resolves silently to Boolean.
    /// </summary>
    [Fact]
    public void Disable_NextLine_ConfigureAwaitAlias_MapsToBoolean()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line ConfigureAwait : equivalent — xUnit");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "ConfigureAwait is a Sprint-166 alias resolving to Boolean (the kind ConfigureAwaitMutator emits)");
    }

    /// <summary>
    /// Sprint 166 (ADR-046 §B): same alias behaviour for <c>AsyncAwait</c> and
    /// <c>AsyncAwaitResult</c> — both resolve to <see cref="Mutator.Boolean"/>.
    /// Case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("// Stryker disable next-line AsyncAwait : reason")]
    [InlineData("// Stryker disable next-line AsyncAwaitResult : reason")]
    [InlineData("// Stryker disable next-line configureawait : reason")] // lowercase
    [InlineData("// Stryker disable next-line CONFIGUREAWAIT : reason")] // uppercase
    public void Disable_NextLine_ClassNameAliases_MapToBoolean(string comment)
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment(comment);

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean);
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

    /// <summary>
    /// ADR-041 Issue 2 verification (Sprint 161, fixes Sprint-160 mistake): when a
    /// PascalCase mutator class name is rejected, the ERR-log hint must point at a
    /// PUBLIC URL (stryker-netx GitHub repo) instead of a project-local path that
    /// doesn't exist in the consuming user's repo. The hint must also inline the 2
    /// most-commonly-confused class-to-kind mappings so the message is self-contained
    /// even without click-through.
    /// </summary>
    /// <remarks>
    /// Sprint 166 (ADR-046 §B) made <c>ConfigureAwait</c> / <c>AsyncAwait</c> /
    /// <c>AsyncAwaitResult</c> RECOGNIZED aliases (no hint fires for those any more).
    /// This test now uses <c>NakedReceiver</c> (a real Stryker mutator class name NOT
    /// in the Sprint-166 alias table) to keep validating the Sprint-161 hint format.
    /// </remarks>
    [Fact]
    public void Disable_NextLine_ClassName_HintIncludesPublicUrl()
    {
        var captured = new List<string>();
        var originalFactory = ApplicationLogging.LoggerFactory;
        ApplicationLogging.LoggerFactory = LoggerFactory.Create(builder =>
        {
            _ = builder.AddProvider(new ListLoggerProvider(captured));
        });

        try
        {
            var ctx = BuildContext();
            var node = NodeWithLeadingComment("// Stryker disable next-line NakedReceiver : reason");

            _ = CommentParser.ParseNodeLeadingComments(node, ctx);

            // The hint must include the public-URL fallback (clickable in modern terminals)
            // so consuming users can navigate to the canonical class-to-kind mapping table.
            captured.Should().Contain(s => s.Contains("github.com/pgm1980/stryker-netx", StringComparison.Ordinal),
                "Sprint 161 (ADR-041 Issue 2): hint must reference the public stryker-netx repo URL");
            captured.Should().Contain(s => s.Contains("disable-comment-syntax.md", StringComparison.Ordinal),
                "the URL must deep-link to the canonical Class-to-Kind mapping document");
            // Self-contained hint: the 2 most-commonly-confused class-to-kind mappings
            // must be inlined so users don't HAVE to click the URL to act on the message.
            captured.Should().Contain(s => s.Contains("ConfigureAwait", StringComparison.Ordinal)
                                        && s.Contains("Boolean", StringComparison.Ordinal),
                "ConfigureAwait → Boolean mapping must be inlined in the hint");
            captured.Should().Contain(s => s.Contains("AsyncAwait", StringComparison.Ordinal),
                "AsyncAwait must also appear inline as the 2nd most-commonly-confused class");
            // The pre-Sprint-161 project-local path must NOT appear: that was the bug
            // that caused user confusion ("file not found in MY repo").
            captured.Should().NotContain(s => s.Contains("see _docs/disable-comment-syntax.md for the Class-to-Kind mapping)", StringComparison.Ordinal),
                "Sprint 161 fixed: project-local path was misleading because users searched for it in THEIR own repo");
        }
        finally
        {
            ApplicationLogging.LoggerFactory = originalFactory;
        }
    }

    /// <summary>
    /// ADR-042 §6 verification (Sprint 162 — fixes my Sprint-160 regression):
    /// <c>// Stryker disable next-line all,Boolean</c> must NOT produce an ERR-log
    /// and MUST short-circuit to all-enum-values (the `all` wildcard wins when it
    /// appears anywhere in the comma-separated list, regardless of other tokens).
    ///
    /// Pre-Sprint-162: ADR-040's `string.Equals(rawMutators, "all", ...)` whole-string
    /// check failed because rawMutators was `"all,Boolean"` (not exact-equal to `"all"`).
    /// The split-and-loop then tried <c>Enum.TryParse&lt;Mutator&gt;("all", ...)</c>
    /// which fails → ERR-log "Unknown mutator kind 'all'" on every run.
    /// </summary>
    [Fact]
    public void Disable_AllInCommaList_AppliesAllKinds()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line all,Boolean : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(
            Enum.GetValues<Mutator>(),
            "ADR-042 §6: `all` anywhere in the comma-list short-circuits to all enum values");
    }

    /// <summary>
    /// ADR-042 §6 case-insensitive verification: `ALL,Boolean` and `boolean,all` must
    /// both work — the short-circuit is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("// Stryker disable next-line ALL,Boolean : reason")]
    [InlineData("// Stryker disable next-line Boolean,all : reason")]
    [InlineData("// Stryker disable next-line Boolean, ALL, Statement : reason")]
    public void Disable_AllInCommaList_CaseInsensitive_AppliesAllKinds(string comment)
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment(comment);

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(Enum.GetValues<Mutator>());
    }

    // ----- Sprint 165 (ADR-045, §5 from Aisess STRYKER_NETX_ANOMALIES_AND_BUGS report) -----
    // Multi-line method-chain // Stryker disable next-line directive scope:
    // the directive must be found when placed between continuation lines of a chain.

    /// <summary>
    /// Sprint 165 helper: parse a multi-line C# snippet wrapped in a class+method and
    /// return the first node of the requested type, so tests can assert on the chain-link
    /// node (e.g., the InvocationExpression representing <c>.ConfigureAwait(false)</c>)
    /// rather than the outer statement.
    /// </summary>
    private static T FindFirst<T>(string methodBody) where T : SyntaxNode
    {
        var source = $"class C {{ void M() {{ {methodBody} }} }}";
        var tree = CSharpSyntaxTree.ParseText(source);
        return tree.GetRoot().DescendantNodes().OfType<T>().First();
    }

    /// <summary>
    /// Helper: find the InvocationExpression whose method-name (rightmost name in the
    /// chain) matches the supplied <paramref name="methodName"/>. Used to target the
    /// specific chain-link the test cares about (e.g., the <c>.ConfigureAwait</c> call
    /// in a multi-link chain).
    /// </summary>
    private static InvocationExpressionSyntax FindInvocationByName(string methodBody, string methodName)
    {
        var source = $"class C {{ void M() {{ {methodBody} }} }}";
        var tree = CSharpSyntaxTree.ParseText(source);
        return tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv =>
                (inv.Expression is MemberAccessExpressionSyntax mae && string.Equals(mae.Name.Identifier.ValueText, methodName, StringComparison.Ordinal)) ||
                (inv.Expression is MemberBindingExpressionSyntax mbe && string.Equals(mbe.Name.Identifier.ValueText, methodName, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Sprint 165 PRIMARY CASE (the Aisess §5 reproducer): a
    /// <c>// Stryker disable next-line Boolean</c> directive placed between two chain links
    /// of a multi-line method-chain (specifically: between the previous call and
    /// <c>.ConfigureAwait(false)</c>) must populate <c>FilteredMutators</c> on the
    /// resulting <c>MutationContext</c>. Pre-Sprint-165 this directive was silently
    /// ignored because the comment trivia is attached to the inner-MAE's operator-token,
    /// which the existing leading-trivia scan didn't reach.
    /// </summary>
    [Fact]
    public void NextLine_Boolean_BetweenChainLinks_AppliesFilter()
    {
        var body = """
            var x = await _repo
                .GetAsync(slug)
                // Stryker disable next-line Boolean : equivalent — xUnit no SyncContext
                .ConfigureAwait(false);
            """;
        var inv = FindInvocationByName(body, "ConfigureAwait");
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(inv, ctx);

        result.FilteredMutators.Should().NotBeNull(
            "mid-chain // Stryker disable next-line must be visible at the InvocationExpression level — Sprint 165 / ADR-045");
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Boolean,
                "the Aisess §5 primary case must filter exactly the Boolean kind");
    }

    /// <summary>
    /// Same Aisess case with <c>// Stryker disable next-line all</c> instead of a
    /// specific kind. Must populate FilteredMutators with EVERY enum value (combined
    /// ADR-042 §6 + ADR-045 fix).
    /// </summary>
    [Fact]
    public void NextLine_All_BetweenChainLinks_AppliesAllKinds()
    {
        var body = """
            var x = await _repo
                .GetAsync(slug)
                // Stryker disable next-line all : equivalent
                .ConfigureAwait(false);
            """;
        var inv = FindInvocationByName(body, "ConfigureAwait");
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(inv, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(
            Enum.GetValues<Mutator>(),
            "mid-chain `// Stryker disable next-line all` must filter every Mutator kind");
    }

    /// <summary>
    /// Sprint 165 LINQ-chain coverage: a <c>// Stryker disable next-line Linq</c>
    /// between <c>.Where(...)</c> and <c>.Select(...)</c> must be visible at the
    /// <c>.Select(...)</c> InvocationExpression level.
    /// </summary>
    [Fact]
    public void NextLine_BetweenLinqChainLinks_AppliesFilter()
    {
        var body = """
            var x = items
                .Where(i => i > 0)
                // Stryker disable next-line Linq : reason
                .Select(i => i * 2);
            """;
        var inv = FindInvocationByName(body, "Select");
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(inv, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Linq);
    }

    /// <summary>
    /// Sprint 165 ConditionalAccess coverage: when the user writes a conditional-access
    /// chain like <c>x?.M()</c>, the comment between <c>x</c> and <c>?.M()</c> is attached
    /// to the <see cref="ConditionalAccessExpressionSyntax"/>'s operator-token. The fix's
    /// direct-chain-link switch must surface this.
    /// </summary>
    [Fact]
    public void NextLine_BeforeConditionalAccess_AppliesFilter()
    {
        var body = """
            var x = _maybeRepo
                // Stryker disable next-line all : reason
                ?.GetAsync(slug);
            """;
        var cae = FindFirst<ConditionalAccessExpressionSyntax>(body);
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(cae, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().BeEquivalentTo(Enum.GetValues<Mutator>());
    }

    /// <summary>
    /// Sprint 165 BinaryExpression coverage: a <c>// Stryker disable next-line Arithmetic</c>
    /// between two operands of a multi-line binary expression must be visible at the
    /// <see cref="BinaryExpressionSyntax"/>'s operator-token (e.g., <c>+</c>).
    /// </summary>
    [Fact]
    public void NextLine_BetweenBinaryOperands_AppliesFilter()
    {
        var body = """
            var x = a
                // Stryker disable next-line Arithmetic : reason
                + b;
            """;
        var bex = FindFirst<BinaryExpressionSyntax>(body);
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(bex, ctx);

        result.FilteredMutators.Should().NotBeNull();
        result.FilteredMutators!.Should().ContainSingle()
            .Which.Should().Be(Mutator.Arithmetic);
    }

    /// <summary>
    /// REGRESSION: single-line chain (no inter-link comment) must still produce no
    /// FilteredMutators when ParseNodeLeadingComments is called on the InvocationExpression.
    /// Verifies that the Sprint-165 helper does not over-apply to single-line cases.
    /// </summary>
    [Fact]
    public void NextLine_SingleLineChain_NoMidComment_NoFilter()
    {
        var body = "var x = _repo.GetAsync(slug).ConfigureAwait(false);";
        var inv = FindInvocationByName(body, "ConfigureAwait");
        var ctx = BuildContext();

        var result = CommentParser.ParseNodeLeadingComments(inv, ctx);

        result.FilteredMutators.Should().BeNull(
            "single-line chains with no mid-link comment must produce no filter — Sprint 165 must not over-apply");
    }

    /// <summary>
    /// REGRESSION: the existing leading-trivia path (directive ABOVE a complete statement)
    /// still works. ADR-045 only EXTENDS the trivia search, it must not break the
    /// pre-existing behaviour for statement-boundary directives.
    /// </summary>
    [Fact]
    public void NextLine_AboveStatement_StillWorks_AfterSprint165()
    {
        var ctx = BuildContext();
        var node = NodeWithLeadingComment("// Stryker disable next-line all : reason");

        var result = CommentParser.ParseNodeLeadingComments(node, ctx);

        result.FilteredMutators.Should().NotBeNull("the existing statement-boundary path must remain functional");
        result.FilteredMutators!.Should().BeEquivalentTo(Enum.GetValues<Mutator>());
    }

    /// <summary>
    /// Sprint 161: minimal in-memory <see cref="ILoggerProvider"/> capturing every
    /// formatted log message into the provided list. Used only by
    /// <see cref="Disable_NextLine_ClassName_HintIncludesPublicUrl"/> to inspect the
    /// hint string emitted by <c>LogLabelNotRecognized</c>.
    /// </summary>
    private sealed class ListLoggerProvider(List<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(sink);
        public void Dispose() { }

        private sealed class ListLogger(List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                sink.Add(formatter(state, exception));
            }
        }
    }
}
