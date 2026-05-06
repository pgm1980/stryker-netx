namespace Stryker.Configuration.Options.Inputs;

/// <summary>
/// Sprint 150 (ADR-031, Bug #8 from Calculator-Tester Bug-Report 4): when the test
/// project references multiple source projects (Clean Architecture: Domain +
/// Infrastructure + App), Stryker historically threw <c>"Test project contains more
/// than one project reference. Please set the project option…"</c>. The user could
/// fall back to <c>--solution &lt;file&gt;.slnx</c> (Sprint 141 advertised that path
/// in the error text), but they explicitly asked for a per-test-project flag that
/// mutates all referenced source projects in one run without requiring a full
/// solution scan.
/// <para>
/// Setting <c>--all-projects</c> tells <c>Stryker.Core.Initialisation.InputFileResolver</c>
/// to accept the multi-reference case as "mutate all of them", returning the full
/// list of <c>SourceProjectInfo</c> instead of throwing the disambiguation
/// exception. The downstream <c>ProjectOrchestrator</c> already iterates the list
/// (used by solution-mode), so no engine-side changes are needed.
/// </para>
/// </summary>
public class AllProjectsInput : Input<bool?>
{
    public override bool? Default => false;

    protected override string Description =>
        @"Mutate ALL source projects referenced by the test project sequentially in a single run.
Useful for Clean-Architecture setups (Domain / Infrastructure / App layers) where a single test
project references multiple source projects. Mutually exclusive with --project (which selects a
single project) and --solution (which scans a whole solution).";

    public bool Validate() => SuppliedInput ?? false;
}
