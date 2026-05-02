#pragma warning disable IDE0028, IDE0300, CA1859 // collection-expression breaks target-type inference; CA1859 perf-not-test-concern (Sprint 28 pattern)
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Testing;
using Stryker.Configuration.Options;
using Stryker.Core.DiffProviders;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.TestHelpers;
using Stryker.TestRunner.Tests;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 103 (v2.89.0) full upstream port from
/// src/Stryker.Core/Stryker.Core.UnitTest/MutantFilters/SinceMutantFilterTests.cs (replaces
/// Sprint 93 placeholder). Production SinceMutantFilter uses IDiffProvider directly with
/// Mock&lt;IDiffProvider&gt; pattern. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class SinceMutantFilterTests : TestBase
{
    /// <summary>Sprint 2 production drift: Mutation has required init properties OriginalNode/
    /// ReplacementNode/DisplayName. Helper avoids per-test boilerplate (Sprint 46 pattern).</summary>
    private static Mutation NewMutation() => new()
    {
        OriginalNode = SyntaxFactory.IdentifierName("a"),
        ReplacementNode = SyntaxFactory.IdentifierName("b"),
        DisplayName = "test",
    };

    [Fact]
    public void ShouldHaveName()
    {
        var diffProviderMock = new Mock<IDiffProvider>(MockBehavior.Loose);

        var target = new SinceMutantFilter(diffProviderMock.Object) as IMutantFilter;

        target.DisplayName.Should().Be("since filter");
    }

    [Fact]
    public void ShouldNotMutateUnchangedFiles()
    {
        var options = new StrykerOptions { Since = false };
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Loose);

        var myFile = Path.Combine("C:/test/", "myfile.cs");

        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult
        {
            ChangedSourceFiles = new Collection<string>(),
            ChangedTestFiles = new Collection<string>(),
        });

        var target = new SinceMutantFilter(diffProvider.Object);
        var file = new CsharpFileLeaf { FullPath = myFile };
        var mutant = new Mutant();

        var filterResult = target.FilterMutants(new List<Mutant> { mutant }, file, options);

        filterResult.Should().BeEmpty();
    }

    [Fact]
    public void ShouldOnlyMutateChangedFiles()
    {
        var options = new StrykerOptions { Since = false };
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Loose);

        var myFile = Path.Combine("C:/test/", "myfile.cs");
        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult
        {
            ChangedSourceFiles = new Collection<string> { myFile },
        });

        var target = new SinceMutantFilter(diffProvider.Object);
        var file = new CsharpFileLeaf { FullPath = myFile };
        var mutant = new Mutant();

        var filterResult = target.FilterMutants(new List<Mutant> { mutant }, file, options);

        filterResult.Should().Contain(mutant);
    }

    [Fact]
    public void ShouldNotFilterMutantsWhereCoveringTestsContainsChangedTestFile()
    {
        var testProjectPath = "C:/MyTests";
        var options = new StrykerOptions();

        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Loose);

        var myTestPath = Path.Combine(testProjectPath, "myTest.cs");
        var tests = new TestSet();
        var test = new TestDescription("id", "name", myTestPath);
        tests.RegisterTests(new[] { test });
        diffProvider.SetupGet(x => x.Tests).Returns(tests);
        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult
        {
            ChangedSourceFiles = new Collection<string> { myTestPath },
            ChangedTestFiles = new Collection<string> { myTestPath },
        });
        var target = new SinceMutantFilter(diffProvider.Object);

        var file = new CsharpFileLeaf { FullPath = Path.Combine("C:/NotMyTests", "myfile.cs") };
        var mutant = new Mutant
        {
            CoveringTests = new TestIdentifierList(new[] { test }),
        };

        var filterResult = target.FilterMutants(new List<Mutant> { mutant }, file, options);

        filterResult.Should().Contain(mutant);
    }

    [Fact]
    public void FilterMutantsWithNoChangedFilesReturnsEmptyList()
    {
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Strict);
        var options = new StrykerOptions();

        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult { ChangedSourceFiles = new List<string>() });
        diffProvider.SetupGet(x => x.Tests).Returns(new TestSet());

        var target = new SinceMutantFilter(diffProvider.Object);

        var mutants = new List<Mutant>
        {
            new() { Id = 1, Mutation = NewMutation() },
            new() { Id = 2, Mutation = NewMutation() },
            new() { Id = 3, Mutation = NewMutation() },
        };

        var results = target.FilterMutants(mutants, new CsharpFileLeaf { RelativePath = "src/1/SomeFile0.cs" }, options);

        results.Count().Should().Be(0);
        mutants.Should().AllSatisfy(m => m.ResultStatus.Should().Be(MutantStatus.Ignored));
        mutants.Should().AllSatisfy(m => m.ResultStatusReason.Should().Be("Mutant not changed compared to target commit"));
    }

    [Fact]
    public void FilterMutantsWithNoChangedFilesAndNoCoverage()
    {
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Strict);
        var options = new StrykerOptions();

        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult { ChangedSourceFiles = new List<string>() });
        diffProvider.SetupGet(x => x.Tests).Returns(new TestSet());

        var target = new SinceMutantFilter(diffProvider.Object);

        var mutants = new List<Mutant>
        {
            new() { Id = 1, Mutation = NewMutation(), ResultStatus = MutantStatus.NoCoverage },
            new() { Id = 2, Mutation = NewMutation(), ResultStatus = MutantStatus.NoCoverage },
            new() { Id = 3, Mutation = NewMutation(), ResultStatus = MutantStatus.NoCoverage },
        };

        var results = target.FilterMutants(mutants, new CsharpFileLeaf { RelativePath = "src/1/SomeFile0.cs" }, options);

        results.Count().Should().Be(0);
        mutants.Should().AllSatisfy(m => m.ResultStatus.Should().Be(MutantStatus.Ignored));
        mutants.Should().AllSatisfy(m => m.ResultStatusReason.Should().Be("Mutant not changed compared to target commit"));
    }

    [Fact]
    public void FilterMutants_FiltersNoMutants_IfTestsChanged()
    {
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Loose);
        var options = new StrykerOptions { WithBaseline = false, ProjectVersion = "version" };

        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult
        {
            ChangedSourceFiles = new List<string>(),
            ChangedTestFiles = new List<string> { "C:/testfile1.cs" },
        });

        var tests = new TestSet();
        var test1 = new TestDescription("id1", "name1", "C:/testfile1.cs");
        var test2 = new TestDescription("id2", "name2", "C:/testfile2.cs");
        tests.RegisterTests(new[] { test1, test2 });
        diffProvider.SetupGet(x => x.Tests).Returns(tests);
        var target = new SinceMutantFilter(diffProvider.Object);
        var testFile1 = new TestIdentifierList(new[] { test1 });
        var testFile2 = new TestIdentifierList(new[] { test2 });

        var expectedToStay1 = new Mutant { CoveringTests = testFile1 };
        var expectedToStay2 = new Mutant { CoveringTests = testFile1 };
        var newMutant = new Mutant { CoveringTests = testFile2 };
        var mutants = new List<Mutant> { expectedToStay1, expectedToStay2, newMutant };

        var results = target.FilterMutants(mutants, new CsharpFileLeaf(), options);

        results.Should().BeEquivalentTo(new[] { expectedToStay1, expectedToStay2 });
    }

    [Fact]
    public void Should_IgnoreMutants_WithoutCoveringTestsInfo_When_Tests_Have_Changed()
    {
        var diffProvider = new Mock<IDiffProvider>(MockBehavior.Loose);
        var options = new StrykerOptions { WithBaseline = false, ProjectVersion = "version" };

        diffProvider.Setup(x => x.ScanDiff()).Returns(new DiffResult
        {
            ChangedSourceFiles = new List<string>(),
            ChangedTestFiles = new List<string> { "C:/testfile.cs" },
        });

        diffProvider.SetupGet(x => x.Tests).Returns(new TestSet());
        var target = new SinceMutantFilter(diffProvider.Object);

        var mutants = new List<Mutant>
        {
            new() { CoveringTests = TestIdentifierList.NoTest() },
        };

        var results = target.FilterMutants(mutants, new CsharpFileLeaf(), options);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Should_ReturnAllMutants_When_NonSourceCodeFile_In_Tests_Has_Changed()
    {
        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var diffProviderMock = new Mock<IDiffProvider>();

        var diffResult = new DiffResult { ChangedTestFiles = new List<string> { "config.json" } };
        diffProviderMock.Setup(x => x.ScanDiff()).Returns(diffResult);

        var target = new SinceMutantFilter(diffProviderMock.Object);
        var mutants = new List<Mutant> { new(), new(), new() };

        var result = target.FilterMutants(mutants, new CsharpFileLeaf { FullPath = "C:\\Foo\\Bar" }, options);

        result.Should().BeEquivalentTo(mutants);
    }
}
