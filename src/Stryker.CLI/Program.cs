using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Stryker.Abstractions.Exceptions;
using Stryker.CLI.Infrastructure;
using Stryker.CLI.Logging;
using Stryker.Configuration;
using Stryker.Core.Infrastructure;

namespace Stryker.CLI;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Microsoft.CodeAnalysis.MSBuildWorkspace requires the .NET SDK MSBuild assemblies to be
        // resolvable from the application context BEFORE MSBuildWorkspace.Create() is called.
        // RegisterDefaults() tells MSBuildLocator to load the SDK MSBuild bundled with the running
        // dotnet runtime (or the latest one installed); without this call, project loading throws
        // "MSB4019: imported project ... was not found" because MSBuild's exe path is unset.
        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

        try
        {
            // Build DI container
            var services = new ServiceCollection()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                .AddStrykerCore()
                .AddStrykerCli()
                .BuildServiceProvider();
            // ensure the logger Factory instance is shared
            ApplicationLogging.LoggerFactory = services.GetRequiredService<ILoggerFactory>();
            var app = services.GetRequiredService<StrykerCli>();
            return await app.RunAsync(args).ConfigureAwait(false);
        }
        catch (NoTestProjectsException exception)
        {
            AnsiConsole.WriteLine(exception.Message);
            return ExitCodes.Success;
        }
        catch (InputException exception)
        {
            AnsiConsole.MarkupLine("[Yellow]Stryker.NET failed to mutate your project. For more information see the logs below:[/]");
            AnsiConsole.WriteLine(exception.ToString());
            return ExitCodes.OtherError;
        }
    }
}
