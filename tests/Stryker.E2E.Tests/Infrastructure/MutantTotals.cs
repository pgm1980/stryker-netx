namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>Per-status mutant counts derived from a <see cref="MutationReport"/>.</summary>
public sealed record MutantTotals(
    int Killed,
    int Survived,
    int NoCoverage,
    int Timeout,
    int RuntimeError,
    int CompileError,
    int Ignored,
    int Pending)
{
    public int Total => Killed + Survived + NoCoverage + Timeout + RuntimeError + CompileError + Ignored + Pending;

    /// <summary>Mutation score per the standard formula, in percent. Returns 0 when the test set is empty.</summary>
    public double MutationScorePercent
    {
        get
        {
            var detectable = Killed + Survived + Timeout;
            return detectable == 0 ? 0.0 : 100.0 * (Killed + Timeout) / detectable;
        }
    }
}
