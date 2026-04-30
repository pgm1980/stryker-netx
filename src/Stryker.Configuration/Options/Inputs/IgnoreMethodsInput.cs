using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stryker.Configuration.Options.Inputs;

public class IgnoreMethodsInput : Input<IEnumerable<string>>
{
    public override IEnumerable<string> Default => Enumerable.Empty<string>();

    protected override string Description => @"Ignore mutations on method parameters.";

    public IEnumerable<Regex> Validate() => SuppliedInput is not null ? ParseRegex(SuppliedInput) : ParseRegex(Default);

    private static List<Regex> ParseRegex(IEnumerable<string> methodPatterns) =>
        methodPatterns
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(methodPattern => new Regex("^(?:[^.]*\\.)*" + Regex.Escape(methodPattern).Replace("\\*", "[^.]*", StringComparison.Ordinal) + "(<[^>]*>)?$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            .ToList();
}
