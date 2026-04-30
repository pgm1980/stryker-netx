using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using Stryker.Abstractions.Options;
using Stryker.Utilities;

namespace Stryker.Configuration;

public readonly struct ExclusionPattern : IExclusionPattern
{
    private static readonly Regex _mutantSpanGroupRegex = new("(?:\\{(?<start>\\d+)\\.\\.(?<end>\\d+)\\})+$", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    private static readonly Regex _mutantSpanRegex = new("\\{(?<start>\\d+)\\.\\.(?<end>\\d+)\\}", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    public ExclusionPattern(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        IsExcluded = s.StartsWith('!');

        var pattern = IsExcluded ? s[1..] : s;
        var mutantSpansRegex = _mutantSpanGroupRegex.Match(pattern);
        if (mutantSpansRegex.Success)
        {
            var filePathPart = pattern[..^mutantSpansRegex.Length];
            var normalized = FilePathUtils.NormalizePathSeparators(filePathPart);
            Glob = Glob.Parse(normalized);

            MutantSpans = _mutantSpanRegex
                .Matches(mutantSpansRegex.Value)
                .Select(x => (int.Parse(x.Groups["start"].Value, CultureInfo.InvariantCulture), int.Parse(x.Groups["end"].Value, CultureInfo.InvariantCulture)));
        }
        else
        {
            var normalized = FilePathUtils.NormalizePathSeparators(pattern);
            Glob = Glob.Parse(normalized);
            MutantSpans = Enumerable.Empty<(int, int)>();
        }
    }

    public bool IsExcluded { get; }

    public Glob Glob { get; }

    public IEnumerable<(int Start, int End)> MutantSpans { get; }
}
