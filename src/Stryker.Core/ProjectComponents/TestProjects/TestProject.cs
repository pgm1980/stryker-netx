using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Core.MutantFilters;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.ProjectComponents.TestProjects;

public sealed class TestProject : IEquatable<ITestProject>, ITestProject
{
    public IProjectAnalysis Analysis { get; }

    public string ProjectFilePath => Analysis.ProjectFilePath;
    public IEnumerable<ITestFile> TestFiles { get; }

    public TestProject(IFileSystem fileSystem, IProjectAnalysis testProjectAnalysis)
    {
        AssertValidTestProject(testProjectAnalysis);

        fileSystem ??= new FileSystem();

        Analysis = testProjectAnalysis;

        var testFiles = new List<TestFile>();
        var preprocessorSymbols = testProjectAnalysis.GetPreprocessorSymbols();
        foreach (var file in testProjectAnalysis.SourceFiles)
        {
            var sourceCode = fileSystem.File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode,
                path: file,
                encoding: Encoding.UTF32,
                options: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, preprocessorSymbols: preprocessorSymbols));

            if (!syntaxTree.IsGenerated())
            {
                testFiles.Add(new TestFile
                {
                    SyntaxTree = syntaxTree,
                    FilePath = file,
                    Source = sourceCode
                });
            }
        }

        TestFiles = testFiles;
    }

    private static void AssertValidTestProject(IProjectAnalysis testProjectAnalysis)
    {
        if (testProjectAnalysis.References.Any(r => r.Contains("Microsoft.VisualStudio.QualityTools.UnitTestFramework")))
        {
            throw new InputException("Please upgrade your test projects to MsTest V2. Stryker.NET uses VSTest which does not support MsTest V1.",
                @"See https://devblogs.microsoft.com/devops/upgrade-to-mstest-v2/ for upgrade instructions.");
        }
    }

    public bool Equals(ITestProject? other) => other is not null && other.Analysis.Equals(Analysis) && other.TestFiles.SequenceEqual(TestFiles);

    public override bool Equals(object? obj) => obj is ITestProject project && Equals(project);

    public override int GetHashCode() => Analysis.GetHashCode();
}
