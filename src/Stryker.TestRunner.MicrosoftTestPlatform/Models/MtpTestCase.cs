using System;
using System.Diagnostics.CodeAnalysis;
using Stryker.Abstractions.Testing;
namespace Stryker.TestRunner.MicrosoftTestPlatform.Models;

/// <summary>
/// MTP-specific implementation of <see cref="ITestCase"/> wrapping a discovered <see cref="TestNode"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MtpTestCase : ITestCase
{
    private readonly TestNode _testNode;

    /// <summary>
    /// Initializes a new instance of <see cref="MtpTestCase"/> from a discovered <see cref="TestNode"/>.
    /// </summary>
    public MtpTestCase(TestNode testNode)
    {
        _testNode = testNode;
        FullyQualifiedName = string.Empty;
        Source = string.Empty;
        AssemblyPath = string.Empty;
    }

    /// <inheritdoc />
    public string FullyQualifiedName { get; init; }

    /// <inheritdoc />
    public Uri Uri => new("executor://MicrosoftTestPlatform");

    /// <inheritdoc />
    public int LineNumber { get; init; }

    /// <inheritdoc />
    public string Source { get; init; }

    /// <inheritdoc />
    public string CodeFilePath => string.Empty;

    /// <inheritdoc />
    public string AssemblyPath { get; init; }

    /// <inheritdoc />
    public Guid Guid { get; init; }

    /// <inheritdoc />
    public string Name => _testNode.DisplayName;

    /// <inheritdoc />
    public string Id => _testNode.Uid;
}
