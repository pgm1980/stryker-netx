using System;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options;
using Stryker.Core.Initialisation;
using Stryker.Solutions;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>
/// Sprint 182 (360°-Analyse H-19): <c>SolutionFile.GetSolution</c> throws an
/// <see cref="InvalidOperationException"/> ("no serializer") for files the solution
/// persistence library does not recognize — that exception was not in the resolver's
/// catch set (IOException/UnauthorizedAccess/Aggregate) and crashed the run raw.
/// An unreadable solution file is a configuration error and must surface as a clean
/// <see cref="InputException"/>.
/// </summary>
public class SolutionLoadGuardTests
{
    [Fact]
    public void ResolveSourceProjectInfos_OnUnrecognizedSolutionFormat_ThrowsInputException()
    {
        var solutionProvider = new Mock<ISolutionProvider>();
        solutionProvider
            .Setup(p => p.GetSolution(It.IsAny<string>()))
            .Throws(new InvalidOperationException("No serializer capable of reading the file."));
        var resolver = new InputFileResolver(
            new MockFileSystem(),
            new Mock<IMSBuildWorkspaceProvider>().Object,
            new Mock<INugetRestoreProcess>().Object,
            solutionProvider.Object,
            NullLogger<InputFileResolver>.Instance);
        var options = new StrykerOptions { SolutionPath = "/repo/broken.sln" };

        var act = () => resolver.ResolveSourceProjectInfos(options);

        act.Should().Throw<InputException>("an unreadable solution file is a user-facing configuration error")
            .WithMessage("*broken.sln*");
    }
}
