using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Reporters.Json.SourceFiles;

public partial class SourceFile : ISourceFile
{
    public string Language { get; init; } = "cs";
    public string Source { get; init; } = string.Empty;
    public ISet<IJsonMutant> Mutants { get; init; } = new HashSet<IJsonMutant>();

    public SourceFile() { }

    public SourceFile(IReadOnlyFileLeaf file, ILogger? logger = null)
    {
        logger ??= ApplicationLogging.LoggerFactory.CreateLogger<SourceFile>();

        Source = file.SourceCode;
        Language = "cs";
        Mutants = new HashSet<IJsonMutant>(new UniqueJsonMutantComparer());

        foreach (var duplicateMutant in file.Mutants
                     .Select(m => new JsonMutant(m))
                     .Where(jsonMutant => !Mutants.Add(jsonMutant)))
        {
            LogDuplicateMutant(logger, duplicateMutant.Id, file.RelativePath);
        }
    }

    public static SourceFile Ignored => new() { Source = "File ignored by mutate filter", Language = "none", Mutants = new HashSet<IJsonMutant>() };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mutant {Id} was generated twice in file {RelativePath}. \nThis should not have happened. Please create an issue at https://github.com/stryker-mutator/stryker-net/issues")]
    private static partial void LogDuplicateMutant(ILogger logger, string id, string relativePath);

    private sealed class UniqueJsonMutantComparer : EqualityComparer<IJsonMutant>
    {
        public override bool Equals(IJsonMutant? left, IJsonMutant? right) => string.Equals(left?.Id, right?.Id, StringComparison.Ordinal);

        public override int GetHashCode(IJsonMutant jsonMutant) => StringComparer.Ordinal.GetHashCode(jsonMutant.Id);
    }
}
