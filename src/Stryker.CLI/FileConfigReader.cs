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
    private static readonly JsonSerializerOptions DeserializeJsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        TypeInfoResolver = FileConfigSerializerContext.Default
    };

    public static void DeserializeConfig(string configFilePath, IStrykerInputs inputs)
    {
        var config = LoadConfig(configFilePath);

        ApplyTopLevelInputs(config, inputs);
        ApplyMutationInputs(config, inputs);
        ApplySinceInputs(config, inputs);
        ApplyBaselineInputs(config, inputs);
        ApplyProjectInfoInputs(config, inputs);
        ApplyThresholdInputs(config, inputs);
    }

    private static void ApplyTopLevelInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        // As json values are first in line we can just overwrite all supplied inputs.
        // For nullable string / collection properties: ONLY forward when the JSON
        // actually contained the key. Forwarding string.Empty / Array.Empty would
        // defeat the per-input null-fallback to Default and cause the validation
        // guards to fire ("incorrect option (<empty>)").
        inputs.ConcurrencyInput.SuppliedInput = config.Concurrency;
        inputs.DisableBailInput.SuppliedInput = config.DisableBail;
        inputs.DisableMixMutantsInput.SuppliedInput = config.DisableMixMutants;
        inputs.AdditionalTimeoutInput.SuppliedInput = config.AdditionalTimeout;
        inputs.BreakOnInitialTestFailureInput.SuppliedInput = config.BreakOnInitialTestFailure;
        if (config.CoverageAnalysis is { } coverageAnalysis) { inputs.CoverageAnalysisInput.SuppliedInput = coverageAnalysis; }
        if (config.Reporters is { } reporters) { inputs.ReportersInput.SuppliedInput = reporters; }
        if (config.Solution is { } solution) { inputs.SolutionInput.SuppliedInput = solution; }
        if (config.Configuration is { } configuration) { inputs.ConfigurationInput.SuppliedInput = configuration; }
        if (config.TargetFramework is { } targetFramework) { inputs.TargetFrameworkInput.SuppliedInput = targetFramework; }
        if (config.Project is { } project) { inputs.SourceProjectNameInput.SuppliedInput = project; }
        if (config.Verbosity is { } verbosity) { inputs.VerbosityInput.SuppliedInput = verbosity; }
        if (config.LanguageVersion is { } languageVersion) { inputs.LanguageVersionInput.SuppliedInput = languageVersion; }
        if (config.TestProjects is { } testProjects) { inputs.TestProjectsInput.SuppliedInput = testProjects; }
        if (config.TestCaseFilter is { } testCaseFilter) { inputs.TestCaseFilterInput.SuppliedInput = testCaseFilter; }
        if (config.TestRunner is { } testRunner) { inputs.TestRunnerInput.SuppliedInput = testRunner; }
        if (config.DashboardUrl is { } dashboardUrl) { inputs.DashboardUrlInput.SuppliedInput = dashboardUrl; }
        if (config.ReportFileName is { } reportFileName) { inputs.ReportFileNameInput.SuppliedInput = reportFileName; }
    }

    private static void ApplyMutationInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.Mutate is { } mutate) { inputs.MutateInput.SuppliedInput = mutate; }
        if (config.MutationLevel is { } mutationLevel) { inputs.MutationLevelInput.SuppliedInput = mutationLevel; }
        if (config.MutationProfile is { } mutationProfile) { inputs.MutationProfileInput.SuppliedInput = mutationProfile; }
        if (config.IgnoreMutations is { } ignoreMutations) { inputs.IgnoreMutationsInput.SuppliedInput = ignoreMutations; }
        if (config.IgnoreMethods is { } ignoreMethods) { inputs.IgnoredMethodsInput.SuppliedInput = ignoreMethods; }
    }

    private static void ApplySinceInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.Since is null)
        {
            return;
        }

        // Since is implicitly enabled when the object exists in the file config
        inputs.SinceInput.SuppliedInput = config.Since.Enabled ?? true;
        if (config.Since.Target is { } sinceTarget) { inputs.SinceTargetInput.SuppliedInput = sinceTarget; }
        if (config.Since.IgnoreChangesIn is { } ignoreChangesIn) { inputs.DiffIgnoreChangesInput.SuppliedInput = ignoreChangesIn; }
    }

    private static void ApplyBaselineInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.Baseline is null)
        {
            return;
        }

        // Baseline is implicitly enabled when the object exists in the file config
        inputs.WithBaselineInput.SuppliedInput = config.Baseline.Enabled ?? true;
        if (config.Baseline.Provider is { } provider) { inputs.BaselineProviderInput.SuppliedInput = provider; }
        if (config.Baseline.FallbackVersion is { } fallbackVersion) { inputs.FallbackVersionInput.SuppliedInput = fallbackVersion; }
        if (config.Baseline.AzureFileShareUrl is { } azureUrl) { inputs.AzureFileStorageUrlInput.SuppliedInput = azureUrl; }
        if (config.Baseline.S3BucketName is { } s3BucketName) { inputs.S3BucketNameInput.SuppliedInput = s3BucketName; }
        if (config.Baseline.S3Endpoint is { } s3Endpoint) { inputs.S3EndpointInput.SuppliedInput = s3Endpoint; }
        if (config.Baseline.S3Region is { } s3Region) { inputs.S3RegionInput.SuppliedInput = s3Region; }
    }

    private static void ApplyProjectInfoInputs(FileBasedInput config, IStrykerInputs inputs)
    {
        if (config.ProjectInfo?.Name is { } projectName) { inputs.ProjectNameInput.SuppliedInput = projectName; }
        if (config.ProjectInfo?.Module is { } moduleName) { inputs.ModuleNameInput.SuppliedInput = moduleName; }
        if (config.ProjectInfo?.Version is { } projectVersion) { inputs.ProjectVersionInput.SuppliedInput = projectVersion; }
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
        IReadOnlyCollection<string> extraKeys = extraData != null ? (IReadOnlyCollection<string>)extraData.Keys : [];
        if (extraKeys.Count > 0)
        {
            var allowedKeys = properties.Select(e => e.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name).OrderBy(e => e, StringComparer.Ordinal);
            var description = extraKeys.Count == 1 ? $"\"{extraKeys.First()}\" was found" : $"others were found (\"{string.Join("\", \"", extraKeys)}\")";
            throw new InputException($"The allowed keys for the \"{namePath}\" object are {{ \"{string.Join("\", \"", allowedKeys)}\" }} but {description} in the config file at \"{configFilePath}\"");
        }
    }
}
