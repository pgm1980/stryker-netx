using System;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Sprint 166 (ADR-046 §B, Aisess Wishlist #6 + §7): central alias table mapping
/// commonly-confused MUTATOR CLASS NAMES (the type name of a <c>MutatorBase</c>
/// subclass) to the user-facing <see cref="Mutator"/> kind the class actually emits.
/// </summary>
/// <remarks>
/// <para>
/// External tutorials, CLAUDE.md notes, and community blog posts about Stryker.NET
/// sometimes refer to mutators by their <em>class name</em> (e.g.
/// <c>ConfigureAwait</c>, <c>AsyncAwait</c>) when describing what a directive
/// affects. The actual filter API uses <see cref="Mutator"/> enum values
/// (e.g. <c>Boolean</c>), so naïve users typing <c>// Stryker disable next-line
/// ConfigureAwait</c> previously produced an ERR-log and a no-op directive.
/// </para>
/// <para>
/// Sprint 161 (ADR-041) mitigated this with a hint URL in the error message.
/// Sprint 166 (ADR-046 §B) goes further: <c>ConfigureAwait</c> / <c>AsyncAwait</c>
/// / <c>AsyncAwaitResult</c> are now ACCEPTED as user-input labels and silently
/// resolved to the underlying kind (<see cref="Mutator.Boolean"/> in all three
/// cases — these mutators emit <c>Type = Mutator.Boolean</c>). Aliases are
/// case-insensitive.
/// </para>
/// <para>
/// The table is intentionally small (3 entries). Adding more aliases here is
/// cheap if user reports surface additional class-name confusion in the wild.
/// </para>
/// </remarks>
internal static class MutatorClassNameAliases
{
    /// <summary>
    /// Resolves a user-supplied label to a <see cref="Mutator"/> kind via the
    /// class-name alias table. Returns <c>false</c> when the label is not a
    /// known alias (caller should then fall back to <c>Enum.TryParse&lt;Mutator&gt;</c>
    /// and finally to the Sprint-161 PascalCase-hint mechanism).
    /// </summary>
    /// <remarks>
    /// Aliases:
    /// <list type="bullet">
    ///   <item><c>ConfigureAwait</c> → <see cref="Mutator.Boolean"/>
    ///         (<c>ConfigureAwaitMutator</c> swaps the boolean argument of <c>.ConfigureAwait(…)</c>).</item>
    ///   <item><c>AsyncAwait</c> → <see cref="Mutator.Boolean"/>
    ///         (<c>AsyncAwaitMutator</c> emits a Boolean-kind mutation via the
    ///         async-await rewrite).</item>
    ///   <item><c>AsyncAwaitResult</c> → <see cref="Mutator.Boolean"/>
    ///         (<c>AsyncAwaitResultMutator</c> — Sprint 16 spec-faithful variant).</item>
    /// </list>
    /// </remarks>
    public static bool TryResolve(string label, out Mutator mutator)
    {
        if (string.IsNullOrEmpty(label))
        {
            mutator = default;
            return false;
        }

        // Case-insensitive match. Could be a Dictionary<string,Mutator> with
        // StringComparer.OrdinalIgnoreCase for larger tables; with 3 entries
        // a switch-pattern is clearer and zero-allocation.
        if (string.Equals(label, "ConfigureAwait", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "AsyncAwait", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "AsyncAwaitResult", StringComparison.OrdinalIgnoreCase))
        {
            mutator = Mutator.Boolean;
            return true;
        }

        mutator = default;
        return false;
    }
}
