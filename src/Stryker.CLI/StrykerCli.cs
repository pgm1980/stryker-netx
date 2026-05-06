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
        // Sprint 149 (Bug #6 from Calculator-Tester Bug-Report 4): accept --reporters
        // (plural) as an alias for --reporter (singular). External tutorials and our own
        // historical docs spell the flag plural; McMaster's "Did you mean: reporter"
        // hint is correct but unfriendly. We rewrite --reporters → --reporter (and the
        // = / : -separated forms) BEFORE McMaster sees the args, so both spellings
        // populate the same ReportersInput.
        args = RewriteReportersAlias(args);

        // Sprint 148 (Bug #4 from Calculator-Tester Bug-Report 4): short-circuit
        // --version / -V before any other parsing. Print the tool version + exit 0.
        if (TryHandleToolVersionFlag(args, out var earlyExitCode))
        {
            await Console.Out.WriteLineAsync(GetToolVersionString()).ConfigureAwait(false);
            return earlyExitCode;
        }

        var app = BuildCommandLineApplication();
        var inputs = new StrykerInputs();
        var cmdConfigReader = new CommandLineConfigReader(_console);

        cmdConfigReader.RegisterCommandLineOptions(app, inputs);
        cmdConfigReader.RegisterInitCommand(app, _fileSystem, inputs, args);

        app.OnExecuteAsync(async (cancellationToken) =>
        {
            PrintStrykerASCIIName();
            _configReader.Build(inputs, args, app, cmdConfigReader);
            _loggingInitializer.SetupLogOptions(inputs);
            _ = PrintStrykerVersionInformationAsync(cmdConfigReader.GetSkipVersionCheckOption(args, app)?.HasValue() ?? false);
            await RunStrykerAsync(inputs).ConfigureAwait(false);
            return ExitCode;
        });

        return await ExecuteWithErrorHandlingAsync(app, args).ConfigureAwait(false);
    }

    /// <summary>
    /// Sprint 149 (Bug #6 from Calculator-Tester Bug-Report 4): rewrites the plural
    /// <c>--reporters</c> alias to the singular <c>--reporter</c> form that
    /// McMaster knows. Handles three argv shapes:
    /// <list type="bullet">
    ///   <item><description><c>--reporters html</c> → <c>--reporter html</c></description></item>
    ///   <item><description><c>--reporters=html</c> → <c>--reporter=html</c></description></item>
    ///   <item><description><c>--reporters:html</c> → <c>--reporter:html</c> (McMaster also accepts colon-separated)</description></item>
    /// </list>
    /// Tippfehler-Variants like <c>--reporterz</c> or <c>--report</c> still fall through
    /// to McMaster's "Did you mean: reporter" hint — we only rewrite the exact plural.
    /// </summary>
    /// <param name="args">Raw argv as passed to <see cref="RunAsync"/>.</param>
    /// <returns>The same array reference if no rewrite was needed, or a new array
    /// with the rewritten entries.</returns>
    internal static string[] RewriteReportersAlias(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var anyRewrite = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (TryRewriteReporterArg(args[i], out var rewritten))
            {
                if (!anyRewrite)
                {
                    args = (string[])args.Clone();
                    anyRewrite = true;
                }
                args[i] = rewritten;
            }
        }
        return args;
    }

    private static bool TryRewriteReporterArg(string arg, out string rewritten)
    {
        if (string.Equals(arg, "--reporters", StringComparison.Ordinal))
        {
            rewritten = "--reporter";
            return true;
        }
        if (arg.StartsWith("--reporters=", StringComparison.Ordinal))
        {
            rewritten = string.Concat("--reporter=", arg.AsSpan("--reporters=".Length));
            return true;
        }
        if (arg.StartsWith("--reporters:", StringComparison.Ordinal))
        {
            rewritten = string.Concat("--reporter:", arg.AsSpan("--reporters:".Length));
            return true;
        }
        rewritten = arg;
        return false;
    }

    private CommandLineApplication BuildCommandLineApplication()
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
        return app;
    }

    private static async Task<int> ExecuteWithErrorHandlingAsync(CommandLineApplication app, string[] args)
    {
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

    /// <summary>
    /// Sprint 148 (Bug #4 from Calculator-Tester Bug-Report 4) supersedes Sprint 141:
    /// detect <c>--version</c> / <c>-V</c> in the raw args BEFORE any other parsing.
    /// Print the tool version on stdout + exit 0 — the .NET-tool platform convention.
    ///
    /// <para>
    /// The Sprint-141 workaround (<c>--tool-version</c> / <c>-T</c>) was rejected by
    /// the Calculator-Tester in Bug-Report 4 as not matching the platform convention.
    /// This sprint frees <c>--version</c> by renaming the historical project-version
    /// flag to <c>--project-version</c> (CommandLineConfigReader.cs Z.237). The
    /// Sprint-141 <c>--tool-version</c>/<c>-T</c> aliases remain functional as a
    /// transitional deprecated path for users who already adopted them — both
    /// surface the same tool-version-string. ADR-029 documents the breaking change
    /// for project-version users.
    /// </para>
    /// </summary>
    private static bool TryHandleToolVersionFlag(string[] args, out int exitCode)
    {
        // Sprint 148 primary: --version / -V; Sprint-141 alias: --tool-version / -T.
        // For --version, only short-circuit if it's the bare flag (no value follows).
        // The legacy `--version <value>` shape now falls through to McMaster which
        // surfaces "Unrecognized option" — the help text shows --project-version as
        // the migration cue.
        for (var i = 0; i < args.Length; i++)
        {
            if (IsToolVersionAliasFlag(args[i]))
            {
                exitCode = ExitCodes.Success;
                return true;
            }
            if (IsBareVersionFlag(args, i))
            {
                exitCode = ExitCodes.Success;
                return true;
            }
        }
        exitCode = 0;
        return false;
    }

    private static bool IsToolVersionAliasFlag(string arg) =>
        string.Equals(arg, "--tool-version", StringComparison.Ordinal)
        || string.Equals(arg, "-T", StringComparison.Ordinal);

    private static bool IsBareVersionFlag(string[] args, int i)
    {
        var a = args[i];
        var isVersion = string.Equals(a, "--version", StringComparison.Ordinal)
                        || string.Equals(a, "-V", StringComparison.Ordinal);
        if (!isVersion)
        {
            return false;
        }
        var nextArg = i + 1 < args.Length ? args[i + 1] : null;
        var hasValue = nextArg is not null && !nextArg.StartsWith('-');
        return !hasValue;
    }

    /// <summary>
    /// Reads the tool's <see cref="AssemblyInformationalVersionAttribute"/> and strips
    /// the trailing <c>+&lt;commit-sha&gt;</c> source-revision build-metadata that
    /// <c>DotNet.ReproducibleBuilds</c> appends, so users see <c>3.1.0</c> not
    /// <c>3.1.0+abcdef0123...</c>.
    /// </summary>
    internal static string GetToolVersionString()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var raw = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? assembly.GetName().Version?.ToString()
                  ?? "0.0.0";
        var plusIndex = raw.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 ? raw[..plusIndex] : raw;
    }
}
