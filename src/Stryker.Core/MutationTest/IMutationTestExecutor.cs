using System.Collections.Generic;
using System.Threading.Tasks;
using Stryker.Abstractions;
using Stryker.Abstractions.Testing;
using static Stryker.Abstractions.Testing.ITestRunner;

namespace Stryker.Core.MutationTest;

/// <summary>
/// Executes exactly one mutation test and stores the result
/// </summary>
public interface IMutationTestExecutor
{
    ITestRunner TestRunner { get; set; }

    Task TestAsync(IProjectAndTests project, IList<IMutant> mutantsToTest, ITimeoutValueCalculator timeoutMs,
        TestUpdateHandler updateHandler);
}
