using System.Collections.Generic;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json.SourceFiles;

namespace Stryker.Core.Dogfood.Tests.Reporters.Json;

/// <summary>Sprint 104 (v2.90.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Reporters/Json/MockJsonReportFileComponent.cs.
/// Test-only stub for BaselineMutantFilterTests JsonReport mutant matching.</summary>
public class MockJsonReportFileComponent : SourceFile
{
    public MockJsonReportFileComponent(string language, string source, ISet<IJsonMutant> mutants)
    {
        Language = language;
        Source = source;
        Mutants = mutants;
    }
}
