using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.TestHelpers;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.SourceProjects;

/// <summary>Sprint 85 (v2.71.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: SourceProjectInfo.AnalyzerResult → Analysis (Sprint 1 Phase 9);
/// Stryker.Utilities.Buildalyzer (upstream) → Stryker.Utilities.MSBuild (ours, Sprint 1 Phase 9).</summary>
public class SourceProjectInfoTests : TestBase
{
    [Fact]
    public void ShouldGenerateProperDefaultCompilationOptions()
    {
        var target = new SourceProjectInfo
        {
            Analysis = TestHelper.SetupProjectAnalyzerResult(
                properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                {
                    { "TargetDir", "/test/bin/Debug/" },
                    { "TargetFileName", "TestName.dll" },
                    { "AssemblyName", "AssemblyName" },
                }).Object,
        };

        var options = target.Analysis.GetCompilationOptions();

        options.AllowUnsafe.Should().BeTrue();
        options.OutputKind.Should().Be(OutputKind.DynamicallyLinkedLibrary);
    }

    [Theory]
    [InlineData("Exe", OutputKind.ConsoleApplication)]
    [InlineData("WinExe", OutputKind.WindowsApplication)]
    [InlineData("AppContainerExe", OutputKind.WindowsRuntimeApplication)]
    public void ShouldGenerateProperCompilationOptions(string kindParam, OutputKind output)
    {
        var target = new SourceProjectInfo
        {
            Analysis = TestHelper.SetupProjectAnalyzerResult(
                properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                {
                    { "AssemblyTitle", "TargetFileName" },
                    { "TargetDir", "/test/bin/Debug/" },
                    { "TargetFileName", "TargetFileName.dll" },
                    { "OutputType", kindParam },
                    { "AssemblyName", "AssemblyName" },
                }).Object,
        };

        var options = target.Analysis.GetCompilationOptions();

        options.AllowUnsafe.Should().BeTrue();
        options.OutputKind.Should().Be(output);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ShouldGenerateProperSigningCompilationOptions(bool signAssembly, bool delaySign)
    {
        var target = new SourceProjectInfo
        {
            Analysis = TestHelper.SetupProjectAnalyzerResult(
                properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                {
                    { "TargetDir", "/test/bin/Debug/" },
                    { "TargetFileName", "TestName.dll" },
                    { "AssemblyName", "AssemblyName" },
                    { "SignAssembly", signAssembly.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    { "DelaySign", delaySign.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    { "AssemblyOriginatorKeyFile", "test/keyfile.snk" },
                }).Object,
        };

        var options = target.Analysis.GetCompilationOptions();

        options.AllowUnsafe.Should().BeTrue();
        options.OutputKind.Should().Be(OutputKind.DynamicallyLinkedLibrary);
        if (signAssembly)
        {
            options.CryptoKeyFile.Should().EndWith("test/keyfile.snk");
        }
        else
        {
            options.CryptoKeyFile.Should().BeNull();
        }
        options.DelaySign.Should().Be(signAssembly ? delaySign : null);
    }

    public static IEnumerable<object?[]> ShouldGenerateProperNullableCompilationOptions_Cases() =>
    [
        ["Nullable", "disable", NullableContextOptions.Disable],
        ["Nullable", "warnings", NullableContextOptions.Warnings],
        ["Nullable", "annotations", NullableContextOptions.Annotations],
        ["Nullable", "enable", NullableContextOptions.Enable],
        ["Nullable", "ENAble", NullableContextOptions.Enable],
        ["Nullable", "WrongValue", NullableContextOptions.Disable],
        ["Nullable", "", NullableContextOptions.Disable],
        ["Nullable", null, NullableContextOptions.Disable],
        ["Nullable", "   ", NullableContextOptions.Disable],
        [null, null, NullableContextOptions.Disable],
    ];

    [Theory]
    [MemberData(nameof(ShouldGenerateProperNullableCompilationOptions_Cases))]
    public void ShouldGenerateProperNullableCompilationOptions(string? key, string? value, NullableContextOptions expectedNullable)
    {
        var properties = new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            { "TargetDir", "/test/bin/Debug/" },
            { "TargetFileName", "TestName.dll" },
            { "AssemblyName", "AssemblyName" },
        };

        if (key is not null)
        {
            properties.Add(key, value ?? string.Empty);
        }

        var target = new SourceProjectInfo
        {
            Analysis = TestHelper.SetupProjectAnalyzerResult(properties: properties).Object,
        };

        var options = target.Analysis.GetCompilationOptions();
        options.NullableContextOptions.Should().Be(expectedNullable);
    }
}
