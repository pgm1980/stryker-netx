using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Serilog.Events;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;
using Stryker.DataCollector;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest.Helpers;
using Stryker.Utilities.Logging;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// Handles VsTest setup and configuration.
/// </summary>
public sealed partial class VsTestContextInformation : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly Func<string, IStrykerTestHostLauncher> _hostBuilder;
    private readonly ILogger _logger;
    private readonly bool _ownVsTestHelper;
    private readonly IVsTestHelper _vsTestHelper;
    private readonly Func<ConsoleParameters, IVsTestConsoleWrapper> _wrapperBuilder;
    private bool _disposed;
    private TestFrameworks _testFramework;

    /// <summary>
    /// Discovered tests (VsTest format)
    /// </summary>
    public IDictionary<Guid, VsTestDescription> VsTests { get; private set; } = new Dictionary<Guid, VsTestDescription>();

    /// <summary>
    /// Tests in each source (assembly)
    /// </summary>
    public IDictionary<string, ISet<Guid>> TestsPerSource { get; } = new Dictionary<string, ISet<Guid>>(StringComparer.Ordinal);

    /// <summary>
    /// Tests (Stryker format)
    /// </summary>
    public TestSet Tests { get; } = new();

    /// <summary>Stryker options used to configure VsTest.</summary>
    public IStrykerOptions Options { get; }

    /// <summary>
    /// Log folder path
    /// </summary>
    public string LogPath =>
        Options.OutputPath == null ? "logs" : _fileSystem.Path.Combine(Options.OutputPath, "logs");

    /// <summary>Initializes a new <see cref="VsTestContextInformation"/>.</summary>
    /// <param name="options">Configuration options</param>
    /// <param name="helper">Optional VsTest helper for path resolution and cleanup.</param>
    /// <param name="fileSystem">Optional file-system abstraction for testability.</param>
    /// <param name="builder">Optional VsTest console wrapper factory for testability.</param>
    /// <param name="hostBuilder">Optional host launcher factory for testability.</param>
    /// <param name="logger">Optional logger.</param>
    public VsTestContextInformation(IStrykerOptions options,
        IVsTestHelper? helper = null,
        IFileSystem? fileSystem = null,
        Func<ConsoleParameters, IVsTestConsoleWrapper>? builder = null,
        Func<string, IStrykerTestHostLauncher>? hostBuilder = null,
        ILogger? logger = null)
    {
        Options = options;
        _ownVsTestHelper = helper == null;
        _fileSystem = fileSystem ?? new FileSystem();
        _vsTestHelper = helper ?? new VsTestHelper(_fileSystem, logger);
        _wrapperBuilder = builder ?? BuildActualVsTestWrapper;
        var devMode = options.DiagMode;
        _hostBuilder = hostBuilder ?? (name => new StrykerVsTestHostLauncher(name, devMode));
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<VsTestContextInformation>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownVsTestHelper)
        {
            _vsTestHelper.Cleanup();
        }
    }

    /// <summary>
    /// Starts a new VsTest instance and returns a wrapper to control it.
    /// </summary>
    /// <param name="runnerId">Name of the instance to create (used in log files)</param>
    /// <param name="controlVariable">name of the env variable storing the active mutation id</param>
    /// <returns>a <see cref="IVsTestConsoleWrapper" /> controlling the created instance.</returns>
    public IVsTestConsoleWrapper BuildVsTestWrapper(string runnerId, string controlVariable)
    {
        var env = DetermineConsoleParameters(runnerId);
        // Set roll forward on no candidate fx so vstest console can start on incompatible dotnet core runtimes
        env.EnvironmentVariables["DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX"] = "2";
        // we define a per runner control variable to prevent conflict
        env.EnvironmentVariables["STRYKER_MUTANT_ID_CONTROL_VAR"] = controlVariable;
        var vsTestConsole = _wrapperBuilder(env);
        try
        {
            vsTestConsole.StartSession();
            vsTestConsole.InitializeExtensions([]);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogVsTestConnectFailed(_logger, e, e.Message);
            throw new GeneralStrykerException("Stryker failed to connect to vstest.console", e);
        }

        return vsTestConsole;
    }

    /// <summary>
    /// Builds a new process launcher used for a test session.
    /// </summary>
    /// <param name="runnerId">Name of the instance to create (used in log files)</param>
    /// <returns>a <see cref="IStrykerTestHostLauncher" /> </returns>
    public IStrykerTestHostLauncher BuildHostLauncher(string runnerId) => _hostBuilder(runnerId);

    private VsTestConsoleWrapper BuildActualVsTestWrapper(ConsoleParameters parameters) =>
        new(_vsTestHelper.GetCurrentPlatformVsTestToolPath(), parameters);

    private ConsoleParameters DetermineConsoleParameters(string runnerId)
    {
        var determineConsoleParameters = new ConsoleParameters
        {
            TraceLevel = Options.LogOptions?.LogLevel switch
            {
                LogEventLevel.Debug => TraceLevel.Verbose,
                LogEventLevel.Verbose => TraceLevel.Verbose,
                LogEventLevel.Error => TraceLevel.Error,
                LogEventLevel.Fatal => TraceLevel.Error,
                LogEventLevel.Warning => TraceLevel.Warning,
                LogEventLevel.Information => TraceLevel.Info,
                _ => TraceLevel.Off
            }
        };

        if (Options.LogOptions?.LogToFile != true)
        {
            return determineConsoleParameters;
        }

        determineConsoleParameters.TraceLevel = Options.DiagMode ? TraceLevel.Verbose : TraceLevel.Info;
        var vsTestLogPath = _fileSystem.Path.Combine(LogPath, $"{runnerId}-log.txt");
        _fileSystem.Directory.CreateDirectory(LogPath);
        determineConsoleParameters.LogFilePath = vsTestLogPath;
        return determineConsoleParameters;
    }

    /// <summary>Returns the <see cref="ITestSet"/> filtered to the supplied source assemblies.</summary>
    public ITestSet GetTestsForSources(IEnumerable<string> sources)
    {
        var result = new TestSet();
        foreach (var source in sources)
        {
            if (TestsPerSource.TryGetValue(source, out var testIdsForSource))
            {
                result.RegisterTests(testIdsForSource.Select(id => Tests[id.ToString()]));
            }
        }

        return result;
    }

    // keeps only test assemblies which have tests.
    /// <summary>Returns only those source assemblies that contain at least one test.</summary>
    public IEnumerable<string> GetValidSources(IEnumerable<string> sources) =>
        sources.Where(s => TestsPerSource.TryGetValue(s, out var result) && result.Count > 0);

    /// <summary>Adds a test source assembly and discovers its tests on first call. Returns true when at least one test was found.</summary>
    public bool AddTestSource(string source, string? frameworkVersion = null, string? platform = null)
    {
        if (!_fileSystem.File.Exists(source))
        {
            throw new GeneralStrykerException(
                $"The test project binaries could not be found at {source}, exiting...");
        }

        if (!TestsPerSource.ContainsKey(source))
        {
            DiscoverTestsInSources(source, frameworkVersion, platform);
        }

        return TestsPerSource[source].Count > 0;
    }

    private void DiscoverTestsInSources(string newSource, string? frameworkVersion = null, string? platform = null)
    {
        var wrapper = BuildVsTestWrapper("TestDiscoverer", "NOT_NEEDED");
        var messages = new List<string>();
        var handler = new DiscoveryEventHandler(messages);
        var settings = GenerateRunSettingsForDiscovery(frameworkVersion, platform);
        wrapper.DiscoverTests([newSource], settings, handler);

        handler.WaitEnd();
        if (handler.Aborted)
        {
            LogDiscoverySettings(_logger, settings);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var combinedMessages = string.Join(Environment.NewLine, messages);
                LogDiscoveryMessages(_logger, combinedMessages);
            }
            LogDiscoveryAborted(_logger);
        }

        wrapper.EndSession();

        TestsPerSource[newSource] = handler.DiscoveredTestCases.Select(c => c.Id).ToHashSet();
        if (VsTests.Count == 0)
        {
            VsTests = new Dictionary<Guid, VsTestDescription>(handler.DiscoveredTestCases.Count);
        }
        foreach (var testCase in handler.DiscoveredTestCases)
        {
            if (!VsTests.TryGetValue(testCase.Id, out var description))
            {
                description = new VsTestDescription(new VsTestCase(testCase));
                VsTests[testCase.Id] = description;
            }

            description.AddSubCase();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogTestCase(_logger, testCase.DisplayName, testCase.Id, testCase.FullyQualifiedName);
            }
        }

        DetectTestFrameworks(VsTests.Values);
        Tests.RegisterTests(VsTests.Values.Select(t => t.Description));
    }

    internal void RegisterDiscoveredTest(VsTestDescription vsTestDescription)
    {
        var id = Guid.Parse(vsTestDescription.Id);
        VsTests[id] = vsTestDescription;
        Tests.RegisterTest(vsTestDescription.Description);
        TestsPerSource[vsTestDescription.Case.Source].Add(id);
    }

    private void DetectTestFrameworks(ICollection<VsTestDescription> tests)
    {
        if (tests == null)
        {
            // 0 = no flags set; MA0099 requires explicit enum value but the [Flags] enum has no zero member
            // and we deliberately preserve upstream semantics (no None member exists in Stryker.Abstractions).
#pragma warning disable MA0099 // Use Explicit enum value — TestFrameworks has no zero member; default cleared state semantics
            _testFramework = 0;
#pragma warning restore MA0099
            return;
        }

        if (tests.Any(testCase => testCase.Framework == TestFrameworks.NUnit))
        {
            _testFramework |= TestFrameworks.NUnit;
        }

        if (tests.Any(testCase => testCase.Framework == TestFrameworks.xUnit))
        {
            _testFramework |= TestFrameworks.xUnit;
        }

        if (tests.Any(testCase => testCase.Framework == TestFrameworks.MsTest))
        {
            _testFramework &= ~TestFrameworks.MsTest;
        }
    }

    private string GenerateCoreSettings(int maxCpu, string? frameworkVersion, string? platform)
    {
        var frameworkConfig = string.IsNullOrWhiteSpace(frameworkVersion)
            ? string.Empty
            : $"<TargetFrameworkVersion>{frameworkVersion}</TargetFrameworkVersion>" + Environment.NewLine;
        // cannot specify AnyCPU or default for VsTest
        var platformConfig = string.IsNullOrWhiteSpace(platform) ||
                             string.Equals(platform, "AnyCPU", StringComparison.Ordinal) ||
                             string.Equals(platform, "Default", StringComparison.Ordinal)
            ? string.Empty
            : $"<TargetPlatform>{SecurityElement.Escape(platform)}</TargetPlatform>" + Environment.NewLine;
        var testCaseFilter = string.IsNullOrWhiteSpace(Options.TestCaseFilter)
            ? string.Empty
            : $"<TestCaseFilter>{SecurityElement.Escape(Options.TestCaseFilter)}</TestCaseFilter>" + Environment.NewLine;
        return
            $@"
<MaxCpuCount>{Math.Max(0, maxCpu)}</MaxCpuCount>
{frameworkConfig}{platformConfig}{testCaseFilter}
<DisableAppDomain>true</DisableAppDomain>";
    }

    private string GenerateRunSettingsForDiscovery(string? frameworkVersion = null, string? platform = null) =>
        $@"<RunSettings>
 <RunConfiguration>
{GenerateCoreSettings(Options.Concurrency, frameworkVersion, platform)}  <DesignMode>true</DesignMode>
 </RunConfiguration>
</RunSettings>";

    /// <summary>Builds the VsTest run settings XML for the given configuration.</summary>
    public string GenerateRunSettings(int? timeout, bool forCoverage, IDictionary<int, ITestIdentifiers>? mutantTestsMap,
        string? helperNameSpace, string? frameworkVersion = null, string? platform = null)
    {
        var settingsForCoverage = string.Empty;
        var needDataCollector = forCoverage || mutantTestsMap is not null;
        var dataCollectorSettings = needDataCollector
            ? CoverageCollector.GetVsTestSettings(
                forCoverage,
                mutantTestsMap?.Select(e => (e.Key, e.Value.GetIdentifiers().Select(x => Guid.Parse(x)))),
                helperNameSpace ?? string.Empty)
            : string.Empty;
        if (_testFramework.HasFlag(TestFrameworks.NUnit))
        {
            settingsForCoverage = "<CollectDataForEachTestSeparately>true</CollectDataForEachTestSeparately>";
        }
        if (_testFramework.HasFlag(TestFrameworks.xUnit) || _testFramework.HasFlag(TestFrameworks.MsTest))
        {
            settingsForCoverage += "<DisableParallelization>true</DisableParallelization>";
        }

        var timeoutSettings = timeout is > 0
            ? $"<TestSessionTimeout>{timeout}</TestSessionTimeout>" + Environment.NewLine
            : string.Empty;

        // we need to block parallel run to capture coverage and when testing multiple mutants in a single run
        var runSettings =
            $@"<RunSettings>
<RunConfiguration>
  <CollectSourceInformation>false</CollectSourceInformation>
{timeoutSettings}{settingsForCoverage}
<DesignMode>false</DesignMode>{GenerateCoreSettings(1, frameworkVersion, platform)}
</RunConfiguration>{dataCollectorSettings}
</RunSettings>";

        return runSettings;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Stryker failed to connect to vstest.console with error: {Error}")]
    private static partial void LogVsTestConnectFailed(ILogger logger, Exception ex, string error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "TestDiscoverer: Discovery settings: {DiscoverySettings}")]
    private static partial void LogDiscoverySettings(ILogger logger, string discoverySettings);

    [LoggerMessage(Level = LogLevel.Debug, Message = "TestDiscoverer: {Messages}")]
    private static partial void LogDiscoveryMessages(ILogger logger, string messages);

    [LoggerMessage(Level = LogLevel.Error, Message = "TestDiscoverer: Test discovery has been aborted!")]
    private static partial void LogDiscoveryAborted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Test Case : name= {DisplayName} (id= {Id}, FQN= {FullyQualifiedName}).")]
    private static partial void LogTestCase(ILogger logger, string displayName, Guid id, string fullyQualifiedName);
}
