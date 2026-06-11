using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Core.Initialisation;
using Stryker.Solutions;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>
/// Sprint 179 (#302-Einzeiler H-05, 360°-Analyse): der Extension-Check in
/// <c>FindProjectFile</c> rief <c>Path.HasExtension</c> auf dem LITERAL-String der
/// Extension statt auf dem Pfad auf — die Bedingung kollabierte zu „Datei existiert"
/// und akzeptierte jede existierende Datei (.sln, .cs, …) als Projektdatei, die dann
/// erst tief in der MSBuild-Analyse undurchsichtig scheiterte.
/// </summary>
public class FindProjectFileTests
{
    private static InputFileResolver BuildResolver(MockFileSystem fileSystem) => new(
        fileSystem,
        new Mock<IMSBuildWorkspaceProvider>().Object,
        new Mock<INugetRestoreProcess>().Object,
        new Mock<ISolutionProvider>().Object,
        NullLogger<InputFileResolver>.Instance);

    [Fact]
    public void FindTestProject_ShouldRejectExistingNonProjectFile()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            ["c:/sol/my.sln"] = new("not a project"),
        });

        var act = () => BuildResolver(fileSystem).FindTestProject("c:/sol/my.sln");

        act.Should().Throw<InputException>().WithMessage("*not a .csproj or .fsproj*");
    }

    [Theory]
    [InlineData("c:/p/My.csproj")]
    [InlineData("c:/p/My.CSPROJ")]
    [InlineData("c:/p/My.fsproj")]
    public void FindTestProject_ShouldAcceptProjectFiles(string path)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [path] = new("<Project/>"),
        });

        BuildResolver(fileSystem).FindTestProject(path).Should().Be(path);
    }
}
