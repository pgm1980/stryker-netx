using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
        // Buildalyzer 9.0.0 targets netstandard2.0 and was compiled against
        // Microsoft.CodeAnalysis 4.0.0.0; stryker-netx ships Microsoft.CodeAnalysis 5.3.0
        // for C# 14 / .NET 10 support. The .NET 10 loader is strict about strong-named
        // assembly versions, so we redirect any 4.0.0.0 lookup to whichever Microsoft.CodeAnalysis*
        // assembly is already present in the load context (5.3.0). Without this, Buildalyzer's
        // EventProcessor throws FileNotFoundException during MSBuild event dispatch and project
        // analysis returns empty results.
        AssemblyLoadContext.Default.Resolving += RedirectMicrosoftCodeAnalysis;

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

    private static Assembly? RedirectMicrosoftCodeAnalysis(AssemblyLoadContext context, AssemblyName name)
    {
        if (name.Name is not "Microsoft.CodeAnalysis" and not "Microsoft.CodeAnalysis.CSharp" and not "Microsoft.CodeAnalysis.VisualBasic")
        {
            return null;
        }

        var alreadyLoaded = context.Assemblies
            .FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.Ordinal));
        if (alreadyLoaded is not null)
        {
            return alreadyLoaded;
        }

        var probe = System.IO.Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
        return System.IO.File.Exists(probe) ? context.LoadFromAssemblyPath(probe) : null;
    }
}
