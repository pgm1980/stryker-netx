using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Json.SourceFiles;
using Xunit;

namespace Stryker.Core.Tests.Reporters.Json;

/// <summary>
/// Sprint 154 (ADR-034) follow-up: regression test for the Sprint-154 source-gen
/// migration to ensure DashboardClient batch publishing
/// (<c>JsonReportSerialization.Options</c> against <c>List&lt;IJsonMutant&gt;</c>) still
/// works after the <c>DefaultJsonTypeInfoResolver</c>-fallback was removed.
/// </summary>
public class JsonReportSerializationDashboardBatchTests
{
    [Fact]
    public void Serialize_ListOfIJsonMutant_DoesNotThrow_AndProducesNonEmptyJson()
    {
        var mutant = new JsonMutant { Id = "1", MutatorName = "test", Replacement = "X", Status = "Killed" };
        var batch = new List<IJsonMutant> { mutant };

        var json = JsonSerializer.Serialize(batch, JsonReportSerialization.Options);

        json.Should().NotBeNullOrEmpty("AOT-trim source-gen must cover List<IJsonMutant>");
        json.Should().Contain("test", "the mutator name should round-trip through the JsonMutantConverter");
    }
}
