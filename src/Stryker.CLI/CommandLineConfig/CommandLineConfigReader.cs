using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Configuration.Options.Inputs;

namespace Stryker.CLI.CommandLineConfig;

public class CommandLineConfigReader
{
    private readonly IAnsiConsole _console;
    private readonly Dictionary<string, CliInput> _cliInputs = new(System.StringComparer.Ordinal);
    private readonly CliInput _configFileInput;
    private readonly CliInput _skipVersionCheckInput;

    public CommandLineConfigReader(IAnsiConsole? console = null)
    {
        _configFileInput = AddCliOnlyInput("config-file", "f", "Choose the file containing your stryker configuration relative to current working directory. Supports json and yaml formats. | default: stryker-config.json", argumentHint: "relative-path");
        _skipVersionCheckInput = AddCliOnlyInput("skip-version-check", null, "Skips check for newer version. | default: false", optionType: CommandOptionType.NoValue, category: InputCategory.Misc);

        _console = console ?? AnsiConsole.Console;
    }

    public void RegisterCommandLineOptions(CommandLineApplication app, IStrykerInputs inputs)
    {
        PrepareCliOptions(inputs);

        RegisterCliInputs(app);
    }

    public void RegisterInitCommand(CommandLineApplication app, IFileSystem fileSystem, IStrykerInputs inputs, string[] args) =>
        app.Command("init", initCommandApp =>
        {
            RegisterCliInputs(initCommandApp);

            initCommandApp.OnExecute(() =>
            {
                _console.WriteLine($"Initializing new config file.");
                _console.WriteLine();

                ReadCommandLineConfig(args[1..], initCommandApp, inputs);
                var configOption = initCommandApp.Options.SingleOrDefault(o => string.Equals(o.LongName, _configFileInput.ArgumentName, System.StringComparison.Ordinal));
                var basePath = fileSystem.Directory.GetCurrentDirectory();
                var configFilePath = Path.Combine(basePath, configOption?.Value() ?? "stryker-config.json");

                if (fileSystem.File.Exists(configFilePath))
                {
                    _console.Write("Config file already exists at ");
                    _console.WriteLine(configFilePath, new Style(Color.Cyan1));
                    var overwrite = _console.Confirm($"Do you want to overwrite it?", false);
                    if (!overwrite)
                    {
                        return;
                    }
                    _console.WriteLine();
                }

                var config = FileConfigGenerator.GenerateConfigAsync(inputs);
                _ = fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(configFilePath) ?? string.Empty);
                fileSystem.File.WriteAllText(configFilePath, config);

                _console.Write("Config file written to ");
                _console.WriteLine(configFilePath, new Style(Color.Cyan1));
                _console.Write("The file is populated with default values, remove the options you don't need and edit the options you want to use. For more information on configuring stryker see: ");
                _console.WriteLine("https://stryker-mutator.io/docs/stryker-net/configuration", new Style(Color.Cyan1));
            });
        });

    public CommandOption? GetConfigFileOption(string[] args, CommandLineApplication app)
    {
        var commands = app.Parse(args);
        return commands.SelectedCommand.Options.SingleOrDefault(o => string.Equals(o.LongName, _configFileInput.ArgumentName, System.StringComparison.Ordinal));
    }

    public CommandOption? GetSkipVersionCheckOption(string[] args, CommandLineApplication app)
    {
        var commands = app.Parse(args);
        return commands.SelectedCommand.Options.SingleOrDefault(o => string.Equals(o.LongName, _skipVersionCheckInput.ArgumentName, System.StringComparison.Ordinal));
    }

    public void ReadCommandLineConfig(string[] args, CommandLineApplication app, IStrykerInputs inputs)
    {
        foreach (var cliInput in app.Parse(args).SelectedCommand.Options.Where(option => option.HasValue()))
        {
            var strykerInput = GetStrykerInput(cliInput);

            if (strykerInput is null)
            {
                continue; // not a stryker input
            }

            switch (cliInput.OptionType)
            {
                case CommandOptionType.NoValue:
                    HandleNoValue((IInput<bool?>)strykerInput);
                    break;

                case CommandOptionType.MultipleValue:
                    HandleMultiValue(cliInput, (IInput<IEnumerable<string>>)strykerInput);
                    break;

                case CommandOptionType.SingleOrNoValue:
                    HandleSingleOrNoValue(strykerInput, cliInput, inputs);
                    break;
            }

            switch (strykerInput)
            {
                case IInput<string> stringInput:
                    HandleSingleStringValue(cliInput, stringInput);
                    break;

                case IInput<int?> nullableIntInput:
                    HandleSingleIntValue(cliInput, nullableIntInput);
                    break;

                case IInput<int> intInput:
                    HandleSingleIntValue(cliInput, (IInput<int?>)intInput);
                    break;
            }
        }
    }

    private void RegisterCliInputs(CommandLineApplication app)
    {
        foreach (var (_, value) in _cliInputs)
        {
            RegisterCliInput(app, value);
        }
    }

    private static void HandleNoValue(IInput<bool?> strykerInput) => strykerInput.SuppliedInput = true;

    private static void HandleSingleStringValue(CommandOption cliInput, IInput<string> strykerInput)
    {
        // Only forward an actual user-supplied value. An unset option must leave
        // SuppliedInput at its default (null) so Input<T>.Validate falls back to
        // the configured Default. Forwarding string.Empty here would defeat that
        // fallback and trip the per-input "incorrect option (<empty>)" guards.
        var value = cliInput.Value();
        if (value is not null)
        {
            strykerInput.SuppliedInput = value;
        }
    }

    private static void HandleSingleIntValue(CommandOption cliInput, IInput<int?> strykerInput)
    {
        if (int.TryParse(cliInput.Value(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            strykerInput.SuppliedInput = value;
        }
        else
        {
            throw new InputException($"Unexpected value for argument {cliInput.LongName}:{cliInput.Value()}. Expected type to be integer");
        }
    }

    private static void HandleSingleOrNoValue(IInput strykerInput, CommandOption cliInput, IStrykerInputs inputs)
    {
        switch (strykerInput)
        {
            // handle single or no value inputs
            case SinceInput sinceInput:
                sinceInput.SuppliedInput = true;
                if (cliInput.Value() is { } sinceTarget)
                {
                    inputs.SinceTargetInput.SuppliedInput = sinceTarget;
                }
                break;

            case WithBaselineInput withBaselineInput:
                withBaselineInput.SuppliedInput = true;
                if (cliInput.Value() is { } baselineTarget)
                {
                    inputs.SinceTargetInput.SuppliedInput = baselineTarget;
                }
                break;

            case OpenReportInput openReportInput:
                if (cliInput.Value() is { } openReport)
                {
                    openReportInput.SuppliedInput = openReport;
                }
                inputs.OpenReportEnabledInput.SuppliedInput = true;
                break;
        }
    }

    private static void HandleMultiValue(CommandOption cliInput, IInput<IEnumerable<string>> strykerInput)
    {
        // Only forward when the option was actually supplied; otherwise leave the
        // SuppliedInput collection at its default null so Input.Validate falls back
        // to the configured Default. Filtering null entries inside .Values guards
        // against spurious null tokens from McMaster's parser.
        if (cliInput.Values.Count == 0)
        {
            return;
        }
        strykerInput.SuppliedInput = (List<string>)[.. cliInput.Values.Where(v => v is not null).Select(v => v!)];
    }

    private IInput? GetStrykerInput(CommandOption cliInput) => cliInput.LongName is null ? null : _cliInputs[cliInput.LongName].Input;

    private void PrepareCliOptions(IStrykerInputs inputs)
    {
        // Category: Generic
        AddCliInput(inputs.ThresholdBreakInput, "break-at", "b", argumentHint: "0-100");
        AddCliInput(inputs.ThresholdHighInput, "threshold-high", "", argumentHint: "0-100");
        AddCliInput(inputs.ThresholdLowInput, "threshold-low", "", argumentHint: "0-100");
        AddCliInput(inputs.LogToFileInput, "log-to-file", "L", optionType: CommandOptionType.NoValue);
        AddCliInput(inputs.VerbosityInput, "verbosity", "V");
        AddCliInput(inputs.ConcurrencyInput, "concurrency", "c", argumentHint: "number");
        AddCliInput(inputs.DisableBailInput, "disable-bail", null, optionType: CommandOptionType.NoValue);
        // Category: Build
        AddCliInput(inputs.SolutionInput, "solution", "s", argumentHint: "file-path", category: InputCategory.Build);
        AddCliInput(inputs.ConfigurationInput, "configuration", null, argumentHint: "Release,Debug", category: InputCategory.Build);
        AddCliInput(inputs.SourceProjectNameInput, "project", "p", argumentHint: "project-name.csproj", category: InputCategory.Build);
        AddCliInput(inputs.TestProjectsInput, "test-project", "tp", CommandOptionType.MultipleValue, InputCategory.Build);
        AddCliInput(inputs.MsBuildPathInput, "msbuild-path", null, category: InputCategory.Build);
        AddCliInput(inputs.TargetFrameworkInput, "target-framework", null, optionType: CommandOptionType.SingleValue, category: InputCategory.Build);
        // Category: Mutation
        AddCliInput(inputs.MutateInput, "mutate", "m", optionType: CommandOptionType.MultipleValue, argumentHint: "glob-pattern", category: InputCategory.Mutation);
        AddCliInput(inputs.MutationLevelInput, "mutation-level", "l", category: InputCategory.Mutation);
        AddCliInput(inputs.MutationProfileInput, "mutation-profile", null, category: InputCategory.Mutation);
        AddCliInput(inputs.SinceInput, "since", "", optionType: CommandOptionType.SingleOrNoValue, argumentHint: "committish", category: InputCategory.Mutation);
        AddCliInput(inputs.WithBaselineInput, "with-baseline", "", optionType: CommandOptionType.SingleOrNoValue, argumentHint: "committish", category: InputCategory.Mutation);
        // Category: Reporting
        AddCliInput(inputs.OpenReportInput, "open-report", "o", CommandOptionType.SingleOrNoValue, argumentHint: "report-type", category: InputCategory.Reporting);
        AddCliInput(inputs.ReportersInput, "reporter", "r", optionType: CommandOptionType.MultipleValue, category: InputCategory.Reporting);
        // Sprint 148 (Bug #4 from Calculator-Tester Bug-Report 4): the project version
        // (dashboard/baseline feature) was historically registered under --version/-v,
        // colliding with the .NET-tool convention that --version prints the tool version.
        // Sprint 141 worked around this with --tool-version/-T as a parallel flag, but
        // the user explicitly rejected that: --version must be the tool version. This
        // sprint frees up --version/-v: the project version moves to --project-version
        // (long-only — no short alias). --version/-v is now handled by TryHandleVersionFlag
        // in StrykerCli (prints tool version + exits 0). Breaking change for CI pipelines
        // that used `--version <value>` for the dashboard — migration: rename to
        // `--project-version <value>`. Documented in ADR-029.
        AddCliInput(inputs.ProjectVersionInput, "project-version", null, category: InputCategory.Reporting);
        AddCliInput(inputs.DashboardApiKeyInput, "dashboard-api-key", null, category: InputCategory.Reporting);
        AddCliInput(inputs.AzureFileStorageSasInput, "azure-fileshare-sas", null, category: InputCategory.Reporting);
        AddCliInput(inputs.S3BucketNameInput, "s3-bucket-name", null, category: InputCategory.Reporting);
        AddCliInput(inputs.S3EndpointInput, "s3-endpoint", null, category: InputCategory.Reporting);
        AddCliInput(inputs.S3RegionInput, "s3-region", null, category: InputCategory.Reporting);
        AddCliInput(inputs.OutputPathInput, "output", "O", optionType: CommandOptionType.SingleValue, category: InputCategory.Reporting);
        // Category: Misc
        AddCliInput(inputs.BreakOnInitialTestFailureInput, "break-on-initial-test-failure", null, optionType: CommandOptionType.NoValue, category: InputCategory.Misc);
        AddCliInput(inputs.DiagModeInput, "diag", null, optionType: CommandOptionType.NoValue, category: InputCategory.Misc);
        AddCliInput(inputs.TestRunnerInput, "test-runner", "t", argumentHint: "vstest,mtp", category: InputCategory.Misc);
    }

    private static void RegisterCliInput(CommandLineApplication app, CliInput option)
    {
        var argumentHint = option.OptionType switch
        {
            CommandOptionType.NoValue => "",
            CommandOptionType.SingleOrNoValue => $"[:<{option.ArgumentHint}>]",
            _ => $" <{option.ArgumentHint}>"
        };

        var commandOption = new StrykerInputOption($"-{option.ArgumentShortName}|--{option.ArgumentName}{argumentHint}", option.OptionType, option.Category)
        {
            Description = option.Description
        };

        app.AddOption(commandOption);
    }

    private CliInput AddCliOnlyInput(string argumentName, string? argumentShortName, string helpText,
        CommandOptionType optionType = CommandOptionType.SingleValue, InputCategory category = InputCategory.Generic, string? argumentHint = null)
    {
        var cliOption = new CliInput
        {
            ArgumentName = argumentName,
            ArgumentShortName = argumentShortName,
            Description = helpText,
            OptionType = optionType,
            ArgumentHint = argumentHint,
            Category = category
        };

        _cliInputs[argumentName] = cliOption;

        return cliOption;
    }

    private void AddCliInput(IInput input, string argumentName, string? argumentShortName,
        CommandOptionType optionType = CommandOptionType.SingleValue, InputCategory category = InputCategory.Generic, string? argumentHint = null)
    {
        var cliOption = new CliInput
        {
            Input = input,
            ArgumentName = argumentName,
            ArgumentShortName = argumentShortName,
            Description = input.HelpText,
            OptionType = optionType,
            Category = category,
            ArgumentHint = argumentHint
        };

        _cliInputs[argumentName] = cliOption;
    }
}

