using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Stryker.Abstractions.Testing;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// VsTest-specific implementation of <see cref="ITestCase"/> backed by a VsTest <see cref="TestCase"/>.
/// </summary>
public sealed class VsTestCase : ITestCase
{
    /// <summary>Initializes a new <see cref="VsTestCase"/> from a VsTest <see cref="TestCase"/>.</summary>
    public VsTestCase(TestCase testCase)
    {
        OriginalTestCase = testCase;
        Id = testCase.Id.ToString();
        Guid = testCase.Id;
        Name = testCase.DisplayName;
        FullyQualifiedName = testCase.FullyQualifiedName;
        Uri = testCase.ExecutorUri;
        CodeFilePath = testCase.CodeFilePath ?? string.Empty;
        LineNumber = testCase.LineNumber;
        Source = testCase.Source;
    }

    /// <summary>Underlying VsTest <see cref="TestCase"/>.</summary>
    public TestCase OriginalTestCase { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public Guid Guid { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Uri Uri { get; }

    /// <inheritdoc />
    public string CodeFilePath { get; }

    /// <inheritdoc />
    public string FullyQualifiedName { get; }

    /// <inheritdoc />
    public int LineNumber { get; }

    /// <inheritdoc />
    public string Source { get; }
}
