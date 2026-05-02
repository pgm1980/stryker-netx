using Xunit;

namespace Stryker.Core.Dogfood.Tests.Helpers;

/// <summary>Sprint 94 (v2.80.0) defer-doc — **wrong-project-assignment**. VsTestHelper lives in
/// `Stryker.TestRunner.VsTest` assembly; tests for it should live in
/// `tests/Stryker.TestRunner.VsTest.Tests/`, not in dogfood. Skip permanently in this project.</summary>
public class VsTestHelperTests
{
    [Fact(Skip = "WRONG PROJECT: VsTestHelper belongs in Stryker.TestRunner.VsTest.Tests, not Dogfood. Permanently skipped here.")]
    public void VsTestHelper_DummyPlaceholder() { /* permanently skipped — wrong project */ }
}
