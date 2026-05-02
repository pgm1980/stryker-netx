using System.Collections.ObjectModel;
using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Configuration.Options;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents;

/// <summary>Sprint 85 (v2.71.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class CsharpProjectComponentTests
{
    [Fact]
    public void ShouldGet100MutationScore()
    {
        var file = new CsharpFileLeaf
        {
            RelativePath = "RootFolder/SomeFile.cs",
            FullPath = "C://RootFolder/SomeFile.cs",
            Mutants = new Collection<Mutant>
            {
                new() { ResultStatus = MutantStatus.Killed },
            },
        };

        var thresholds = new Thresholds { High = 100, Low = 50, Break = 0 };

        file.GetMutationScore().Should().Be(1);
        file.CheckHealth(thresholds).Should().Be(Health.Good);
    }

    [Fact]
    public void ShouldGet0MutationScore()
    {
        var file = new CsharpFileLeaf
        {
            RelativePath = "RootFolder/SomeFile.cs",
            FullPath = "C://RootFolder/SomeFile.cs",
            Mutants = new Collection<Mutant>
            {
                new() { ResultStatus = MutantStatus.Survived },
            },
        };

        file.GetMutationScore().Should().Be(0);

        var thresholdsDanger = new Thresholds { High = 80, Low = 1, Break = 0 };
        file.CheckHealth(thresholdsDanger).Should().Be(Health.Danger);
        var thresholdsWarning = new Thresholds { High = 80, Low = 0, Break = 0 };
        file.CheckHealth(thresholdsWarning).Should().Be(Health.Warning);
    }

    [Fact]
    public void ShouldGet50MutationScore()
    {
        var file = new CsharpFileLeaf
        {
            RelativePath = "RootFolder/SomeFile.cs",
            FullPath = "C://RootFolder/SomeFile.cs",
            Mutants = new Collection<Mutant>
            {
                new() { ResultStatus = MutantStatus.Survived },
                new() { ResultStatus = MutantStatus.Killed },
            },
        };

        file.GetMutationScore().Should().Be(0.5);

        var thresholdsDanger = new Thresholds { High = 80, Low = 60, Break = 0 };
        file.CheckHealth(thresholdsDanger).Should().Be(Health.Danger);
        var thresholdsWarning = new Thresholds { High = 80, Low = 50, Break = 0 };
        file.CheckHealth(thresholdsWarning).Should().Be(Health.Warning);
        var thresholdsGood = new Thresholds { High = 50, Low = 49, Break = 0 };
        file.CheckHealth(thresholdsGood).Should().Be(Health.Good);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScoreNaN_NoMutations()
    {
        var target = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = [] });

        var result = target.GetMutationScore();
        result.Should().Be(double.NaN);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScore_OneMutation()
    {
        var target = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed } } });

        var result = target.GetMutationScore();
        result.Should().Be(1);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScore_TwoFolders()
    {
        var target = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed } } });
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived } } });

        var result = target.GetMutationScore();
        result.Should().Be(0.5);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScore_Recursive()
    {
        var target = new CsharpFolderComposite();
        var subFolder = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed } } });
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived } } });
        target.Add(subFolder);
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived }, new() { ResultStatus = MutantStatus.Killed } } });

        var result = target.GetMutationScore();
        result.Should().Be(0.5);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScore_Recursive2()
    {
        var target = new CsharpFolderComposite();
        var subFolder = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived } } });
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived } } });
        target.Add(subFolder);
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived }, new() { ResultStatus = MutantStatus.Killed } } });
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed }, new() { ResultStatus = MutantStatus.Killed } } });
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Survived }, new() { ResultStatus = MutantStatus.Killed } } });
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed }, new() { ResultStatus = MutantStatus.Killed } } });
        subFolder.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Killed }, new() { ResultStatus = MutantStatus.Killed } } });

        var result = target.GetMutationScore();
        result.Should().BeApproximately(0.6666666666666666, 1e-15);
    }

    [Theory]
    [InlineData(MutantStatus.Killed, 1)]
    [InlineData(MutantStatus.Timeout, 1)]
    [InlineData(MutantStatus.Survived, 0)]
    [InlineData(MutantStatus.Pending, double.NaN)]
    public void ReportComponent_ShouldCalculateMutationScore_OnlyKilledIsSuccessful(MutantStatus status, double expectedScore)
    {
        var target = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = status } } });

        var result = target.GetMutationScore();
        result.Should().Be(expectedScore);
    }

    [Fact]
    public void ReportComponent_ShouldCalculateMutationScore_BuildErrorIsNull()
    {
        var target = new CsharpFolderComposite();
        target.Add(new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.CompileError } } });

        var result = target.GetMutationScore();
        result.Should().Be(double.NaN);
    }

    [Fact]
    public void ShouldGetNaNMutationScoreWhenAllExcluded()
    {
        var file = new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.Ignored } } };

        file.GetMutationScore().Should().Be(double.NaN);
    }

    [Fact]
    public void ShouldGet0MutationScoreWhenAllNoCoverage()
    {
        var file = new CsharpFileLeaf { Mutants = new Collection<Mutant> { new() { ResultStatus = MutantStatus.NoCoverage } } };

        file.GetMutationScore().Should().Be(0);
    }
}
