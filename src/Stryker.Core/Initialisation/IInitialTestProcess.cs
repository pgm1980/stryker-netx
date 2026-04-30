using System.Threading.Tasks;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;

namespace Stryker.Core.Initialisation;

public interface IInitialTestProcess
{
    Task<InitialTestRun> InitialTestAsync(IStrykerOptions options, IProjectAndTests project, ITestRunner testRunner);
}
