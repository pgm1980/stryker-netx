using System;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Reporters;

public class FilteredMutantsLogger
{
    private readonly ILogger<FilteredMutantsLogger> _logger;

    public FilteredMutantsLogger(ILogger<FilteredMutantsLogger>? logger = null)
    {
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<FilteredMutantsLogger>();
    }

    public void OnMutantsCreated(IReadOnlyProjectComponent reportComponent)
    {
        var skippedMutants = reportComponent.Mutants.Where(m => m.ResultStatus != MutantStatus.Pending);

        var skippedMutantGroups = skippedMutants.GroupBy(x => new { x.ResultStatus, x.ResultStatusReason }).OrderBy(x => x.Key.ResultStatusReason, StringComparer.Ordinal);

        foreach (var skippedMutantGroup in skippedMutantGroups)
        {
            var format = FormatStatusReasonLogString(skippedMutantGroup.Count(), skippedMutantGroup.Key.ResultStatus);
            var message = string.Format(CultureInfo.InvariantCulture, format, skippedMutantGroup.Count(), skippedMutantGroup.Key.ResultStatus, skippedMutantGroup.Key.ResultStatusReason);
            _logger.LogInformation("{Message}", message);
        }

        if (skippedMutants.Any())
        {
            var format = LeftPadAndFormatForMutantCount(skippedMutants.Count(), "total mutants are skipped for the above mentioned reasons");
            var message = string.Format(CultureInfo.InvariantCulture, format, skippedMutants.Count());
            _logger.LogInformation("{Message}", message);
        }

        var notRunMutantsWithResultStatusReason = reportComponent.Mutants
            .Where(m => m.ResultStatus == MutantStatus.Pending && !string.IsNullOrEmpty(m.ResultStatusReason))
            .GroupBy(x => x.ResultStatusReason, StringComparer.Ordinal);

        foreach (var notRunMutantReason in notRunMutantsWithResultStatusReason)
        {
            var format = LeftPadAndFormatForMutantCount(notRunMutantReason.Count(), "mutants will be tested because: {1}");
            var message = string.Format(CultureInfo.InvariantCulture, format, notRunMutantReason.Count(), notRunMutantReason.Key);
            _logger.LogInformation("{Message}", message);
        }

        var notRunCount = reportComponent.Mutants.Count(m => m.ResultStatus == MutantStatus.Pending);
        var totalFormat = LeftPadAndFormatForMutantCount(notRunCount, "total mutants will be tested");
        var totalMessage = string.Format(CultureInfo.InvariantCulture, totalFormat, notRunCount);
        _logger.LogInformation("{Message}", totalMessage);
    }

    private static string FormatStatusReasonLogString(int mutantCount, MutantStatus resultStatus)
    {
        // Pad for status CompileError length
        var padForResultStatusLength = 13 - resultStatus.ToString().Length;

        var formattedString = LeftPadAndFormatForMutantCount(mutantCount, "mutants got status {1}.");
        formattedString += "Reason: {2}".PadLeft(11 + padForResultStatusLength);

        return formattedString;
    }

    private static string LeftPadAndFormatForMutantCount(int mutantCount, string logString)
    {
        // Pad for max 5 digits mutant amount
        var padLengthForMutantCount = 5 - mutantCount.ToString(CultureInfo.InvariantCulture).Length;
        return "{0} " + logString.PadLeft(logString.Length + padLengthForMutantCount);
    }
}
