using System.Collections.Generic;
using System.IO;
using Moq;
using Stryker.Core.Helpers.ProcessUtil;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/MockExtensions.cs. Framework-agnostic
/// Moq helpers used by upstream <c>Stryker.Core.UnitTest</c> consumers.
/// </summary>
public static class MockExtensions
{
    /// <summary>
    /// Wires a default <see cref="ProcessResult"/> return value onto every
    /// <see cref="IProcessExecutor.Start"/> overload signature so individual
    /// tests can assert against the supplied output / exit code without each
    /// having to repeat the verbose Moq setup boilerplate.
    /// </summary>
    public static void SetupProcessMockToReturn(this Mock<IProcessExecutor> processExecutorMock, string result, int exitCode = 0) =>
        processExecutorMock.Setup(x => x.Start(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
            It.IsAny<int>()))
        .Returns(new ProcessResult
        {
            ExitCode = exitCode,
            Output = result.Replace('\\', Path.DirectorySeparatorChar),
        });
}
