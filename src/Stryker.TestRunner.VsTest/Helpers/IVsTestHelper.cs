namespace Stryker.TestRunner.VsTest.Helpers;

/// <summary>
/// Locates a VsTest console binary suitable for the current platform, deploying the embedded copy when necessary.
/// </summary>
public interface IVsTestHelper
{
    /// <summary>Returns the path to the VsTest console binary for the current OS platform.</summary>
    string GetCurrentPlatformVsTestToolPath();

    /// <summary>Removes any VsTest binaries that were deployed by stryker-netx.</summary>
    void Cleanup(int tries = 5);
}
