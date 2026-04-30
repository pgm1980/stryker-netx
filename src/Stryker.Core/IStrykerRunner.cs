using System.Threading.Tasks;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;

namespace Stryker.Core;

public interface IStrykerRunner
{
    Task<StrykerRunResult> RunMutationTestAsync(IStrykerInputs inputs);
}
