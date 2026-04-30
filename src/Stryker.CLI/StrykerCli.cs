using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Spectre.Console;
using Stryker.CLI.Clients;
using Stryker.CLI.CommandLineConfig;
using Stryker.CLI.Logging;
using Stryker.Configuration;
using Stryker.Configuration.Options;
using Stryker.Core;

namespace Stryker.CLI;

public partial class StrykerCli
{
    private readonly IStrykerRunner _stryker;
    private readonly IConfigBuilder _configReader;
    private readonly ILoggingInitializer _loggingInitializer;
    private readonly IStrykerNugetFeedClient _nugetClient;
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;

    public int ExitCode { get; private set; } = ExitCodes.Success;

    public StrykerCli(
        IStrykerRunner stryker,
        IConfigBuilder configReader,
        ILoggingInitializer loggingInitializer,
        IStrykerNugetFeedClient nugetClient,
        IAnsiConsole console,
        IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(stryker);
        ArgumentNullException.ThrowIfNull(configReader);
        ArgumentNullException.ThrowIfNull(loggingInitializer);
        ArgumentNullException.ThrowIfNull(nugetClient);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(fileSystem);

        _stryker = stryker;
        _configReader = configReader;
        _loggingInitializer = loggingInitializer;
        _nugetClient = nugetClient;
        _console = console;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Analyzes the arguments and displays an interface to the user. Kicks off the program.
    /// </summary>
    /// <param name="args">User input</param>
    public async Task<int> RunAsync(string[] args)
    {
        var app = new CommandLineApplication(new ConsoleWrapper(_console))
        {
            Name = "Stryker",
            FullName = "Stryker: Stryker mutator for .Net",
            Description = "Stryker mutator for .Net",
            ExtendedHelpText = "Welcome to Stryker for .Net! Run dotnet stryker to kick off a mutation test run",
            HelpTextGenerator = new GroupedHelpTextGenerator()
        };
        _ = app.HelpOption();

        var inputs = new StrykerInputs();
        var cmdConfigReader = new CommandLineConfigReader(_console);

        cmdConfigReader.RegisterCommandLineOptions(app, inputs);
        cmdConfigReader.RegisterInitCommand(app, _fileSystem, inputs, args);

        app.OnExecuteAsync(async (cancellationToken) =>
        {
            // app started
            PrintStrykerASCIIName();

            _configReader.Build(inputs, args, app, cmdConfigReader);
            _loggingInitializer.SetupLogOptions(inputs);

            // Print version info. Don't await, let it run in the background for performance reasons
            _ = PrintStrykerVersionInformationAsync(cmdConfigReader.GetSkipVersionCheckOption(args, app)?.HasValue() ?? false);
            await RunStrykerAsync(inputs).ConfigureAwait(false);
            return ExitCode;
        });

        try
        {
            return await app.ExecuteAsync(args).ConfigureAwait(false);
        }
        catch (CommandParsingException ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);

            if (ex is UnrecognizedCommandParsingException uex && uex.NearestMatches.Any())
            {
                await Console.Error.WriteLineAsync().ConfigureAwait(false);
                await Console.Error.WriteLineAsync("Did you mean this?").ConfigureAwait(false);
                foreach (var match in uex.NearestMatches)
                {
                    await Console.Error.WriteLineAsync("    " + match).ConfigureAwait(false);
                }
            }

            return ExitCodes.OtherError;
        }
    }

    private async Task RunStrykerAsync(IStrykerInputs inputs)
    {
        var result = await _stryker.RunMutationTestAsync(inputs).ConfigureAwait(false);

        HandleStrykerRunResult(result);
    }

    private void HandleStrykerRunResult(StrykerRunResult result)
    {
        var logger = ApplicationLogging.LoggerFactory.CreateLogger<StrykerCli>();

        if (double.IsNaN(result.MutationScore))
        {
            LogUnableToCalculateScore(logger);
        }
        else
        {
            LogFinalMutationScore(logger, result.MutationScore);
        }

        if (result.ScoreIsLowerThanThresholdBreak())
        {
            var thresholdBreak = (double)result.Options.Thresholds.Break / 100;
            LogScoreBelowThresholdBreak(logger);

            _console.WriteLine();
            _console.MarkupLine($"[Red]The mutation score is lower than the configured break threshold of {thresholdBreak:P0}.[/]");
            _console.MarkupLine(" [Red]Looks like you've got some work to do :smiling_face_with_halo:[/]");

            ExitCode = ExitCodes.BreakThresholdViolated;
        }
    }

    private void PrintStrykerASCIIName()
    {
        _console.MarkupLine(@"[Yellow]
   _____ _              _               _   _ ______ _______
  / ____| |            | |             | \ | |  ____|__   __|
 | (___ | |_ _ __ _   _| | _____ _ __  |  \| | |__     | |
  \___ \| __| '__| | | | |/ / _ \ '__| | . ` |  __|    | |
  ____) | |_| |  | |_| |   <  __/ |    | |\  | |____   | |
 |_____/ \__|_|   \__, |_|\_\___|_| (_)|_| \_|______|  |_|
                   __/ |
                  |___/
[/]");
        _console.WriteLine();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is for sonarcloud which Roslyn does not know about.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3168:\"async\" methods should not return \"void\"", Justification = "This method is fire and forget. Task.Run also doesn't work in unit tests")]
    private async Task PrintStrykerVersionInformationAsync(bool skipVersionCheck)
    {
        var logger = ApplicationLogging.LoggerFactory.CreateLogger<StrykerCli>();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!SemanticVersion.TryParse(version ?? string.Empty, out var currentVersion))
        {
            if (string.IsNullOrEmpty(version))
            {
                LogMissingAttribute(logger, nameof(AssemblyInformationalVersionAttribute), assembly, assembly.Location);
            }
            else
            {
                LogFailedToParseVersion(logger, version);
            }
            return;
        }

        _console.MarkupLine(string.Create(CultureInfo.InvariantCulture, $"Version: [Green]{currentVersion}[/]"));
        LogStrykerStarting(logger, currentVersion);
        _console.WriteLine();

        if (skipVersionCheck)
        {
            return;
        }

        var latestVersion = await _nugetClient.GetLatestVersionAsync().ConfigureAwait(false);
        if (latestVersion > currentVersion)
        {
            _console.MarkupLine(string.Create(CultureInfo.InvariantCulture, $@"[Yellow]A new version of stryker-netx ({latestVersion}) is available. Please consider upgrading using `dotnet tool update -g dotnet-stryker-netx`[/]"));
            _console.WriteLine();
        }
        else
        {
            var previewVersion = await _nugetClient.GetPreviewVersionAsync().ConfigureAwait(false);
            if (previewVersion > currentVersion)
            {
                _console.MarkupLine(string.Create(CultureInfo.InvariantCulture, $@"[Cyan]A preview version of stryker-netx ({previewVersion}) is available.
If you would like to try out this preview version you can install it with `dotnet tool update -g dotnet-stryker-netx --version {previewVersion}`
Since this is a preview feature things might not work as expected! Please report any findings on GitHub![/]"));
                _console.WriteLine();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stryker was unable to calculate a mutation score")]
    private static partial void LogUnableToCalculateScore(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "The final mutation score is {MutationScore:P2}")]
    private static partial void LogFinalMutationScore(ILogger logger, double mutationScore);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Final mutation score is below threshold break. Crashing...")]
    private static partial void LogScoreBelowThresholdBreak(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Attribute} is missing in {Assembly} at {AssemblyLocation}")]
    private static partial void LogMissingAttribute(ILogger logger, string attribute, Assembly assembly, string assemblyLocation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse version {Version} as a semantic version")]
    private static partial void LogFailedToParseVersion(ILogger logger, string version);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stryker starting, version: {Version}")]
    private static partial void LogStrykerStarting(ILogger logger, SemanticVersion version);
}
