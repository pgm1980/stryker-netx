namespace Stryker.Abstractions;

/// <summary>
/// Sprint 166 (ADR-046 §C, Aisess Wishlist #9): pipeline-phase enumeration for the
/// <c>--break-after</c> diagnostic flag. Selecting a non-<see cref="None"/> value
/// instructs Stryker to perform every pipeline phase UP TO AND INCLUDING the named
/// boundary, then terminate cleanly without proceeding to the (expensive) per-mutant
/// test loop.
/// </summary>
/// <remarks>
/// <para>
/// Use case (per Aisess Anomalies Report § 10 wishlist item #9): the user has a
/// 3 600-test suite where each diagnostic mutation run pays ≈ 9 minutes of initial
/// test discovery before any actionable output appears. Setting
/// <c>--break-after build</c> (or <c>analysis</c>) lets the user verify project /
/// build configuration in ≈ 30 seconds instead of 9 minutes.
/// </para>
/// <para>
/// Values are ordered by pipeline depth: a higher numeric value means a later
/// break-point. Comparison is via the standard enum-ordinal so callers can express
/// "have we reached or passed phase X?" with a simple <c>&gt;=</c>.
/// </para>
/// </remarks>
public enum BreakAfterPhase
{
    /// <summary>
    /// Default. Stryker runs the full pipeline including the mutation test loop.
    /// No early termination.
    /// </summary>
    None = 0,

    /// <summary>
    /// Break after project analysis (mutable-project discovery, target-framework
    /// resolution, source-project filter validation). Diagnoses bad
    /// <c>--project</c> / <c>--solution</c> / <c>--mutate</c> configuration before
    /// any build investment.
    /// </summary>
    Analysis = 1,

    /// <summary>
    /// Break after initial build (MSBuild compile of source + test projects).
    /// Diagnoses build errors / missing references before paying for test discovery.
    /// </summary>
    Build = 2,

    /// <summary>
    /// Break after initial test run (test discovery + baseline test execution that
    /// validates the suite is green before mutation begins). Diagnoses test
    /// failures, missing adapters, or `Category!=Integration`-style filter issues
    /// (see ADR-044 + § 4 of the Aisess Anomalies Report) without paying for
    /// mutation generation.
    /// </summary>
    InitialTestRun = 3,

    /// <summary>
    /// Break after per-project mutation generation (mutators run, mutants
    /// catalogued, filters applied at the orchestrator level). Diagnoses
    /// mutation-coverage decisions — what WOULD have been tested — without
    /// paying for the per-mutant test loop. Per <c>--reporter html</c>, this
    /// emits a partial HTML report listing the would-be mutants.
    /// </summary>
    MutationGeneration = 4,
}
