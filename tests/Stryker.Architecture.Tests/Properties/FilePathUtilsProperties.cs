using System.IO;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stryker.Utilities;

namespace Stryker.Architecture.Tests.Properties;

/// <summary>
/// Property-based tests for <see cref="FilePathUtils.NormalizePathSeparators(string?)"/>.
/// Verifies invariants the path normalisation must satisfy on every platform.
/// </summary>
public sealed class FilePathUtilsProperties
{
    [Property]
    public Property Normalising_Null_Returns_Null()
    {
        var result = FilePathUtils.NormalizePathSeparators(null);
        return (result is null).ToProperty();
    }

    [Property]
    public Property Normalising_Twice_Is_Idempotent(NonEmptyString input)
    {
        var once = FilePathUtils.NormalizePathSeparators(input.Get);
        var twice = FilePathUtils.NormalizePathSeparators(once);
        return string.Equals(once, twice, System.StringComparison.Ordinal).ToProperty();
    }

    [Property]
    public Property Normalising_Preserves_Length(NonEmptyString input)
    {
        var normalised = FilePathUtils.NormalizePathSeparators(input.Get);
        return (normalised is not null && normalised.Length == input.Get.Length).ToProperty();
    }

    [Property]
    public Property Normalised_Path_Has_No_Alt_Separator_When_Different_From_Primary(NonEmptyString input)
    {
        if (Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar)
        {
            return true.ToProperty();
        }

        var normalised = FilePathUtils.NormalizePathSeparators(input.Get);
        return (normalised is not null && !normalised.Contains(Path.AltDirectorySeparatorChar)).ToProperty();
    }
}
