using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Abstractions.Testing;
using Stryker.Core.MutationTest;

namespace Stryker.Core.Initialisation;

public interface IProjectOrchestrator : IDisposable
{
    Task<IEnumerable<IMutationTestProcess>> MutateProjectsAsync(IStrykerOptions options, IReporter reporters, ITestRunner? runner = null);
}
