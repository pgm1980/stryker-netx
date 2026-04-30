using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stryker.CLI;

public static class FileConfigReader
{
    private static readonly JsonSerializerOptions DeserializeJsonOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip };

    public static void DeserializeConfig(string configFilePath, IStrykerInputs inputs)
    {
        var config = LoadConfig(configFilePath);

        ApplyTopLevelInputs(config, inputs);
        ApplySinceInputs(config, inputs);
        ApplyBaselineInputs(config, inputs);
        ApplyProjectInfoInputs(config, inputs);
        ApplyThresholdInputs(config, inputs);
    }

    private static void ApplyTopLevelInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        // As json values are first in line we can just overwrite all supplied inputs
        inputs.ConcurrencyInput.SuppliedInput = config.Concurrency;
        inputs.CoverageAnalysisInput.SuppliedInput = config.CoverageAnalysis ?? string.Empty;
        inputs.DisableBailInput.SuppliedInput = config.DisableBail;
        inputs.DisableMixMutantsInput.SuppliedInput = config.DisableMixMutants;
        inputs.AdditionalTimeoutInput.SuppliedInput = config.AdditionalTimeout;
        inputs.MutateInput.SuppliedInput = config.Mutate ?? Array.Empty<string>();
        inputs.MutationLevelInput.SuppliedInput = config.MutationLevel ?? string.Empty;
        inputs.ReportersInput.SuppliedInput = config.Reporters ?? Array.Empty<string>();
        inputs.SolutionInput.SuppliedInput = config.Solution ?? string.Empty;
        inputs.ConfigurationInput.SuppliedInput = config.Configuration ?? string.Empty;
        inputs.TargetFrameworkInput.SuppliedInput = config.TargetFramework ?? string.Empty;
        inputs.SourceProjectNameInput.SuppliedInput = config.Project ?? string.Empty;
        inputs.VerbosityInput.SuppliedInput = config.Verbosity ?? string.Empty;
        inputs.LanguageVersionInput.SuppliedInput = config.LanguageVersion ?? string.Empty;
        inputs.TestProjectsInput.SuppliedInput = config.TestProjects ?? Array.Empty<string>();
        inputs.TestCaseFilterInput.SuppliedInput = config.TestCaseFilter ?? string.Empty;
        inputs.TestRunnerInput.SuppliedInput = config.TestRunner ?? string.Empty;
        inputs.DashboardUrlInput.SuppliedInput = config.DashboardUrl ?? string.Empty;
        inputs.IgnoreMutationsInput.SuppliedInput = config.IgnoreMutations ?? Array.Empty<string>();
        inputs.IgnoredMethodsInput.SuppliedInput = config.IgnoreMethods ?? Array.Empty<string>();
        inputs.ReportFileNameInput.SuppliedInput = config.ReportFileName ?? string.Empty;
        inputs.BreakOnInitialTestFailureInput.SuppliedInput = config.BreakOnInitialTestFailure;
    }

    private static void ApplySinceInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.Since is null)
        {
            return;
        }

        // Since is implicitly enabled when the object exists in the file config
        inputs.SinceInput.SuppliedInput = config.Since.Enabled ?? true;
        inputs.SinceTargetInput.SuppliedInput = config.Since.Target ?? string.Empty;
        inputs.DiffIgnoreChangesInput.SuppliedInput = config.Since.IgnoreChangesIn ?? Array.Empty<string>();
    }

    private static void ApplyBaselineInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.Baseline is null)
        {
            return;
        }

        // Baseline is implicitly enabled when the object exists in the file config
        inputs.WithBaselineInput.SuppliedInput = config.Baseline.Enabled ?? true;
        inputs.BaselineProviderInput.SuppliedInput = config.Baseline.Provider ?? string.Empty;
        inputs.FallbackVersionInput.SuppliedInput = config.Baseline.FallbackVersion ?? string.Empty;
        inputs.AzureFileStorageUrlInput.SuppliedInput = config.Baseline.AzureFileShareUrl ?? string.Empty;
        inputs.S3BucketNameInput.SuppliedInput = config.Baseline.S3BucketName ?? string.Empty;
        inputs.S3EndpointInput.SuppliedInput = config.Baseline.S3Endpoint ?? string.Empty;
        inputs.S3RegionInput.SuppliedInput = config.Baseline.S3Region ?? string.Empty;
    }

    private static void ApplyProjectInfoInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        inputs.ProjectNameInput.SuppliedInput = config.ProjectInfo?.Name ?? string.Empty;
        inputs.ModuleNameInput.SuppliedInput = config.ProjectInfo?.Module ?? string.Empty;
        inputs.ProjectVersionInput.SuppliedInput = config.ProjectInfo?.Version ?? string.Empty;
    }

    private static void ApplyThresholdInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        inputs.ThresholdBreakInput.SuppliedInput = config.Thresholds?.Break;
        inputs.ThresholdHighInput.SuppliedInput = config.Thresholds?.High;
        inputs.ThresholdLowInput.SuppliedInput = config.Thresholds?.Low;
    }

    private static FileBasedInput LoadConfig(string configFilePath)
    {
        using var streamReader = new StreamReader(configFilePath);
        var fileContents = streamReader.ReadToEnd();

        FileBasedInput input;
        try
        {
            FileBasedInputOuter? root;
            if (configFilePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || configFilePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                root = DeserializeYaml(fileContents);
            }
            else if (configFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                root = DeserializeJson(fileContents);
            }
            else
            {
                throw new InputException($"Unknown file type for config file at \"{configFilePath}\"");
            }

            if (root == null)
            {
                throw new InputException($"The config file at \"{configFilePath}\" could not be parsed.");
            }
            input = root.Input ?? throw new InputException($"The config file at \"{configFilePath}\" must contain a single \"stryker-config\" root object.");
        }
        catch (JsonException jsonException)
        {
            throw new InputException($"The config file at \"{configFilePath}\" could not be parsed.", jsonException.Message);
        }

        EnsureCorrectKeys(configFilePath, input, "stryker-config");

        return input;
    }

    private static FileBasedInputOuter? DeserializeJson(string json) => JsonSerializer.Deserialize<FileBasedInputOuter>(json, DeserializeJsonOptions);

    private static FileBasedInputOuter DeserializeYaml(string yaml)
    {
        var yamldeserializer = new DeserializerBuilder()
                                .IgnoreUnmatchedProperties()
                                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                                .Build();

        return yamldeserializer.Deserialize<FileBasedInputOuter>(yaml);
    }

    private static void EnsureCorrectKeys(string configFilePath, IExtraData @object, string namePath)
    {
        var properties = @object.GetType().GetProperties().Where(e => e.GetCustomAttribute<JsonPropertyNameAttribute>() != null).ToList();
        foreach (var property in properties.Where(property => property.PropertyType.IsAssignableTo(typeof(IExtraData))))
        {
            if (property.GetValue(@object) is IExtraData child)
            {
                EnsureCorrectKeys(configFilePath, child, $"{namePath}.{property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name}");
            }
        }
        var extraData = @object.ExtraData;
        IReadOnlyCollection<string> extraKeys = extraData != null ? (IReadOnlyCollection<string>)extraData.Keys : Array.Empty<string>();
        if (extraKeys.Count > 0)
        {
            var allowedKeys = properties.Select(e => e.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name).OrderBy(e => e, StringComparer.Ordinal);
            var description = extraKeys.Count == 1 ? $"\"{extraKeys.First()}\" was found" : $"others were found (\"{string.Join("\", \"", extraKeys)}\")";
            throw new InputException($"The allowed keys for the \"{namePath}\" object are {{ \"{string.Join("\", \"", allowedKeys)}\" }} but {description} in the config file at \"{configFilePath}\"");
        }
    }
}
