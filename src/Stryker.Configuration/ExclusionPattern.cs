using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using Stryker.Abstractions.Options;
using Stryker.Utilities;

namespace Stryker.Configuration;

public readonly partial struct ExclusionPattern : IExclusionPattern
{
    [GeneratedRegex(@"(?:\{(?<start>\d+)\.\.(?<end>\d+)\})+$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MutantSpanGroupRegex();

    [GeneratedRegex(@"\{(?<start>\d+)\.\.(?<end>\d+)\}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MutantSpanRegex();

    public ExclusionPattern(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        IsExcluded = s.StartsWith('!');

        var pattern = IsExcluded ? s[1..] : s;
        var mutantSpansRegex = MutantSpanGroupRegex().Match(pattern);
        if (mutantSpansRegex.Success)
        {
            var filePathPart = pattern[..^mutantSpansRegex.Length];
            var normalized = FilePathUtils.NormalizePathSeparators(filePathPart);
            Glob = Glob.Parse(normalized);

            MutantSpans = MutantSpanRegex()
                .Matches(mutantSpansRegex.Value)
                .Select(x => (int.Parse(x.Groups["start"].Value, CultureInfo.InvariantCulture), int.Parse(x.Groups["end"].Value, CultureInfo.InvariantCulture)));
        }
        else
        {
            var normalized = FilePathUtils.NormalizePathSeparators(pattern);
            Glob = Glob.Parse(normalized);
            MutantSpans = [];
        }
    }

    public bool IsExcluded { get; }

    public Glob Glob { get; }

    public IEnumerable<(int Start, int End)> MutantSpans { get; }
}
