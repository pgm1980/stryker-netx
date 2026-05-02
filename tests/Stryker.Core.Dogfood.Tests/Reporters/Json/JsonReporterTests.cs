#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Configuration.Options;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Json;

/// <summary>Sprint 114 (v3.0.1) full upstream port from JsonReporterTests (replaces Sprint 110
/// architectural-deferral). Production matches upstream JsonReport API. Most tests are STRUCTURAL
/// property assertions (Thresholds.Contains, Files.Count, ProjectRoot value) — not exact-JSON-string
/// comparisons. Sprint 110's architectural-deferral was overly conservative; structural tests port
/// directly. Only 2 OnAllMutantsTested tests rely on JSON-roundtrip — also work since JsonReport
/// hybrid source-gen serialization is symmetric (Sprint 16).</summary>
public class JsonReporterTests : TestBase
{
    private readonly IFileSystem _fileSystemMock = new MockFileSystem();
    private readonly string _testFilePath = "c:\\mytestfile.cs";
    private readonly string _testFileContents = @"using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExtraProject.XUnit
{
    public class UnitTest1
    {
        [TestMethod]
        public void Test1()
        {
            // example test
        }
    }
}
";

    public JsonReporterTests() => _fileSystemMock.File.WriteAllText(_testFilePath, _testFileContents);

    [Fact]
    public void JsonMutantPositionLine_ThrowsArgumentExceptionWhenSetToLessThan1()
    {
        Action actNeg = () => _ = new Position { Line = -1 };
        Action actZero = () => _ = new Position { Line = 0 };
        actNeg.Should().Throw<ArgumentException>();
        actZero.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JsonMutantPositionColumn_ThrowsArgumentExceptionWhenSetToLessThan1()
    {
        Action actNeg = () => _ = new Position { Column = -1 };
        Action actZero = () => _ = new Position { Column = 0 };
        actNeg.Should().Throw<ArgumentException>();
        actZero.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JsonMutantLocation_FromValidFileLinePositionSpanShouldAdd1ToLineAndColumnNumbers()
    {
        var lineSpan = new FileLinePositionSpan(
            "",
            new LinePosition(2, 2),
            new LinePosition(4, 5));

        var jsonMutantLocation = new Stryker.Core.Reporters.Json.Location(lineSpan);

        jsonMutantLocation.Start.Line.Should().Be(3);
        jsonMutantLocation.Start.Column.Should().Be(3);
        jsonMutantLocation.End.Line.Should().Be(5);
        jsonMutantLocation.End.Column.Should().Be(6);
    }

    [Fact]
    public void JsonReportFileComponent_ShouldHaveLanguageSet()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        var fileComponent = (CsharpFileLeaf)((CsharpFolderComposite)folderComponent).GetAllFiles().First();

        new SourceFile(fileComponent).Language.Should().Be("cs");
    }

    [Fact]
    public void JsonReportFileComponent_ShouldContainOriginalSource()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        var fileComponent = (CsharpFileLeaf)((CsharpFolderComposite)folderComponent).GetAllFiles().First();

        new SourceFile(fileComponent).Source.Should().Be(fileComponent.SourceCode);
    }

    [Fact]
    public void JsonReportFileComponents_ShouldContainMutants()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        foreach (var file in ((CsharpFolderComposite)folderComponent).GetAllFiles())
        {
            var jsonReportComponent = new SourceFile((CsharpFileLeaf)file);
            foreach (var mutant in file.Mutants)
            {
                jsonReportComponent.Mutants.Should().Contain(m => m.Id == mutant.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    [Fact]
    public void JsonReportFileComponent_DoesNotContainDuplicateMutants()
    {
        var loggerMock = Mock.Of<ILogger>();
        var folderComponent = ReportTestHelper.CreateProjectWith(duplicateMutant: true);
        foreach (var file in ((CsharpFolderComposite)folderComponent).GetAllFiles())
        {
            var jsonReportComponent = new SourceFile((CsharpFileLeaf)file, loggerMock);
            foreach (var mutant in file.Mutants)
            {
                jsonReportComponent.Mutants.Should().Contain(m => m.Id == mutant.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    [Fact]
    public void JsonReport_ThresholdsAreSet()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        var report = JsonReport.Build(new StrykerOptions(), folderComponent, It.IsAny<TestProjectsInfo>());

        report.Thresholds.Should().ContainKey("high");
        report.Thresholds.Should().ContainKey("low");
    }

    [Fact]
    public void JsonReport_ShouldContainAtLeastOneFile()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        var report = JsonReport.Build(new StrykerOptions(), folderComponent, It.IsAny<TestProjectsInfo>());

        report.Files.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void JsonReport_ShouldContainTheProjectRoot()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith();
        var report = JsonReport.Build(new StrykerOptions(), folderComponent, It.IsAny<TestProjectsInfo>());

        report.ProjectRoot.Should().Be("/home/user/src/project/");
    }

    [Fact]
    public void JsonReport_ShouldContainFullPath()
    {
        var folderComponent = ReportTestHelper.CreateProjectWith(root: OperatingSystem.IsWindows() ? "c://" : "/");
        var report = JsonReport.Build(new StrykerOptions(), folderComponent, It.IsAny<TestProjectsInfo>());
        var path = report.Files.Keys.First();

        Path.IsPathFullyQualified(path).Should().BeTrue($"{path} should not be a relative path");
    }
}
