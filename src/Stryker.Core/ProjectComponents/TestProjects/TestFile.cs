using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions.ProjectComponents;

namespace Stryker.Core.ProjectComponents.TestProjects;

public sealed class TestFile : IEquatable<ITestFile>, ITestFile
{
    public required SyntaxTree SyntaxTree { get; init; }
    public required string FilePath { get; init; }
    public required string Source { get; init; }
    public IList<ITestCase> Tests { get; private set; } = [];

    public void AddTest(string id, string name, SyntaxNode node)
    {
        if (Tests.Any(test => string.Equals(test.Id, id, StringComparison.Ordinal)))
        {
            return;
        }

        Tests.Add(new TestCase
        {
            Id = id,
            Name = name,
            Node = node
        });
    }

    public bool Equals(ITestFile? other) => other != null && other.FilePath.Equals(FilePath, StringComparison.Ordinal) && other.Source.Equals(Source, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is TestFile file && Equals(file);

    // Stryker disable once bitwise: Bitwise mutation does not change functional usage of GetHashCode
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(FilePath) ^ StringComparer.Ordinal.GetHashCode(Source);
}
