using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Core.MutationTest;

namespace Stryker.Core.Initialisation;

public interface IProjectMutator
{
    IMutationTestProcess MutateProject(IStrykerOptions options, MutationTestInput input, IReporter reporters, IMutationTestProcess? mutationTestProcess = null);
}
