using System;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions.ProjectComponents;

namespace Stryker.Core.ProjectComponents.TestProjects;

public sealed class TestCase : IEquatable<ITestCase>, ITestCase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required SyntaxNode Node { get; init; }

    public bool Equals(ITestCase? other) => other is not null && string.Equals(other.Id, Id, StringComparison.Ordinal) && string.Equals(other.Name, Name, StringComparison.Ordinal) && other.Node.Span == Node.Span;

    public override bool Equals(object? obj) => obj is ITestCase @case && Equals(@case);

    public override int GetHashCode() => (Id, Name).GetHashCode();
}
