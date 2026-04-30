using System.Collections.Generic;
using System.Threading.Tasks;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.MutationTest;

public interface IMutationTestProcess
{
    MutationTestInput Input { get; }
    void Initialize(MutationTestInput input, IStrykerOptions options, IReporter reporter);
    void Mutate();
    Task<StrykerRunResult> TestAsync(IEnumerable<IMutant> mutantsToTest);
    void Restore();
    void GetCoverage();
    void FilterMutants();
}
