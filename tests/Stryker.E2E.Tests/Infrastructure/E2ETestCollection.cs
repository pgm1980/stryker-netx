using Xunit;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Shared xUnit collection so every E2E test class joins the same
/// sequential-execution group AND shares a single <see cref="BuildFixture"/>
/// + <see cref="StrykerRunCacheFixture"/> instance across the whole test run.
/// Without this, each class would get its own fixture and re-run Stryker for
/// every profile.
/// </summary>
[CollectionDefinition(Name)]
public sealed class E2ETestCollection : ICollectionFixture<BuildFixture>, ICollectionFixture<StrykerRunCacheFixture>
{
    public const string Name = "E2E-Sequential";
}
