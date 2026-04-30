using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.Options;

namespace Stryker.Utilities.MSBuild;

/// <summary>
/// C#-specific extensions for <see cref="IProjectAnalysis"/> — produce Roslyn
/// <see cref="CSharpParseOptions"/> and <see cref="CSharpCompilationOptions"/>
/// instances configured from the project's MSBuild evaluation. This is the
/// stryker-netx replacement for <c>IAnalyzerResultCSharpExtensions</c>.
///
/// Sprint 2.3: extension members (C# 14) — instance members are grouped inside
/// an <c>extension(IProjectAnalysis projectAnalysis)</c> block. The compiler
/// still emits classic static entry points, so existing callers continue to
/// work without changes.
/// </summary>
public static class IProjectAnalysisCSharpExtensions
{
    private const string InterceptorsNamespacesKey = "InterceptorsNamespaces";
    private const string InterceptorsPreviewNamespacesKey = "InterceptorsPreviewNamespaces";

    extension(IProjectAnalysis projectAnalysis)
    {
        /// <summary>
        /// Returns the preprocessor symbols defined for the project, derived from the
        /// MSBuild <c>DefineConstants</c> property (semicolon-separated). Returns an empty
        /// enumeration when no constants are defined.
        /// </summary>
        public IEnumerable<string> GetPreprocessorSymbols()
        {
            ArgumentNullException.ThrowIfNull(projectAnalysis);
            var defineConstants = projectAnalysis.GetPropertyOrDefault("DefineConstants");
            if (string.IsNullOrWhiteSpace(defineConstants))
            {
                return [];
            }
            return defineConstants
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s));
        }

        /// <summary>
        /// Builds <see cref="CSharpCompilationOptions"/> matching the project's MSBuild
        /// evaluation (output kind, nullable context, unsafe blocks, signing, diagnostic
        /// suppressions, warning level, etc.).
        /// </summary>
        public CSharpCompilationOptions GetCompilationOptions()
        {
            ArgumentNullException.ThrowIfNull(projectAnalysis);

            var compilationOptions = new CSharpCompilationOptions(projectAnalysis.GetOutputKind())
                .WithNullableContextOptions(projectAnalysis.GetNullableContextOptions())
                .WithAllowUnsafe(projectAnalysis.GetPropertyOrDefault("AllowUnsafeBlocks", true))
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithConcurrentBuild(true)
                .WithModuleName(projectAnalysis.GetAssemblyName())
                .WithOverflowChecks(projectAnalysis.GetPropertyOrDefault("CheckForOverflowUnderflow", false))
                .WithSpecificDiagnosticOptions(projectAnalysis.GetDiagnosticOptions())
                .WithWarningLevel(projectAnalysis.GetWarningLevel());

            if (projectAnalysis.IsSignedAssembly() && projectAnalysis.GetAssemblyOriginatorKeyFile() is { } keyFile)
            {
                compilationOptions = compilationOptions.WithCryptoKeyFile(keyFile)
                    .WithStrongNameProvider(new DesktopStrongNameProvider())
                    .WithDelaySign(projectAnalysis.IsDelayedSignedAssembly());
            }
            return compilationOptions;
        }

        /// <summary>
        /// Builds <see cref="CSharpParseOptions"/> matching the project's MSBuild
        /// evaluation (preprocessor symbols, language version, custom <c>Features</c>,
        /// interceptors namespaces).
        /// </summary>
        public CSharpParseOptions GetParseOptions(IStrykerOptions options)
        {
            ArgumentNullException.ThrowIfNull(projectAnalysis);
            ArgumentNullException.ThrowIfNull(options);

            var parseOptions = new CSharpParseOptions(
                options.LanguageVersion,
                DocumentationMode.None,
                preprocessorSymbols: projectAnalysis.GetPreprocessorSymbols()
            );

            var features = ExtractCSharpFeatures(projectAnalysis);

            if (features.Count > 0)
            {
                parseOptions = parseOptions.WithFeatures(features);
            }

            return parseOptions;
        }

        private NullableContextOptions GetNullableContextOptions()
        {
            Enum.TryParse(projectAnalysis.GetPropertyOrDefault("Nullable", "disable"), true, out NullableContextOptions nullableOptions);
            return nullableOptions;
        }
    }

    /// <summary>
    /// The &lt;Features&gt; MSBuild property is an internal Roslyn mechanism that passes a key-value dictionary
    /// directly to <see cref="CSharpParseOptions.WithFeatures"/>. It is not publicly documented by Microsoft as
    /// it is primarily intended for internal compiler development. Interceptors are a use case relying on this
    /// mechanism, using the features <c>InterceptorsNamespaces</c> and <c>InterceptorsPreviewNamespaces</c>.
    /// </summary>
    private static List<KeyValuePair<string, string>> ExtractCSharpFeatures(IProjectAnalysis projectAnalysis)
    {
        var features = new List<KeyValuePair<string, string>>();

        var projectFeatures = projectAnalysis.GetPropertyOrDefault("Features");
        if (!string.IsNullOrWhiteSpace(projectFeatures))
        {
            foreach (var feature in projectFeatures.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedFeature = feature.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedFeature))
                {
                    features.Add(new KeyValuePair<string, string>(trimmedFeature, "true"));
                }
            }
        }

        var interceptorsNamespaces = new List<string?>
        {
            projectAnalysis.GetPropertyOrDefault(InterceptorsNamespacesKey),
            projectAnalysis.GetPropertyOrDefault(InterceptorsPreviewNamespacesKey)
        };
        var combinedNamespaces = string.Join(";", interceptorsNamespaces.Where(ns => !string.IsNullOrWhiteSpace(ns)));

        if (!string.IsNullOrWhiteSpace(combinedNamespaces))
        {
            features.Add(new KeyValuePair<string, string>(InterceptorsNamespacesKey, combinedNamespaces));
        }

        return features;
    }
}
