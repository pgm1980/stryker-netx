using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// Marker interface for stryker-controlled VsTest host launchers.
/// </summary>
public interface IStrykerTestHostLauncher : ITestHostLauncher
{
}
