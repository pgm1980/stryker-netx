// Partially borrowed from https://github.com/Testura/Testura.Mutation/blob/ca2785dba8997ab814be4bb69113739db357810f/src/Testura.Mutation.Core/Execution/Compilation/EmbeddedResourceCreator.cs
// Modernized for stryker-netx (C# 14 / .NET 10): nullable annotations, file-scoped namespace,
// CultureInfo.InvariantCulture, StringComparison.Ordinal-overloads, surgical S3011-pragma for
// the single Roslyn-internal-field reflection call.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Resources.NetStandard;
using Microsoft.CodeAnalysis;
using Mono.Cecil;

namespace Stryker.Utilities.EmbeddedResources;

[ExcludeFromCodeCoverage]
public static class EmbeddedResourcesGenerator
{
    private static readonly ConcurrentDictionary<string, List<(ResourceDescription description, object? context)>> _resourceDescriptions = new(StringComparer.Ordinal);

    public static void ResetCache()
    {
        _resourceDescriptions.Clear();
    }

    public static IEnumerable<ResourceDescription> GetManifestResources(string assemblyPath, string projectFilePath, string rootNamespace, IEnumerable<string> embeddedResources)
    {
        if (!_resourceDescriptions.ContainsKey(projectFilePath))
        {
            using var module = LoadModule(assemblyPath);
            if (module is not null)
            {
                _resourceDescriptions.TryAdd(projectFilePath, [.. ReadResourceDescriptionsFromModule(module)]);
            }

            // Failed to load some or all resources from module, generate missing resources from disk
            if (module is not null && _resourceDescriptions[projectFilePath].Count < embeddedResources.Count())
            {
                var existing = _resourceDescriptions[projectFilePath];
                var missingEmbeddedResources = embeddedResources.Where(r =>
                    existing.Any(fr => string.Equals(GetResourceDescriptionInternalName(fr.description), GenerateResourceName(r), StringComparison.Ordinal)));
                _resourceDescriptions[projectFilePath] = [.. existing.Concat(GenerateManifestResources(projectFilePath, rootNamespace, missingEmbeddedResources))];
            }

            // Failed to load module, generate all resources from disk
            if (module is null)
            {
                _resourceDescriptions.TryAdd(projectFilePath, [.. GenerateManifestResources(projectFilePath, rootNamespace, embeddedResources)]);
            }
        }

        if (!_resourceDescriptions.TryGetValue(projectFilePath, out var resourcesDescription))
        {
            yield break;
        }
        foreach (var description in resourcesDescription)
        {
            yield return description.description;
        }
    }

    private static ModuleDefinition? LoadModule(string assemblyPath)
    {
        try
        {
            return ModuleDefinition.ReadModule(
                assemblyPath,
                new ReaderParameters(ReadingMode.Deferred)
                {
                    InMemory = true,
                    ReadWrite = false,
                    AssemblyResolver = new CrossPlatformAssemblyResolver(),
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static IEnumerable<(ResourceDescription, object?)> ReadResourceDescriptionsFromModule(ModuleDefinition module)
    {
        foreach (var moduleResource in module.Resources.Where(r => r.ResourceType == ResourceType.Embedded).Cast<EmbeddedResource>())
        {
            var shortLivedBackingStream = moduleResource.GetResourceStream();

            var resourceStream = new MemoryStream();
            shortLivedBackingStream.CopyTo(resourceStream);

            // reset streams back to start
            resourceStream.Position = 0;
            shortLivedBackingStream.Position = 0;

            yield return (new ResourceDescription(
                moduleResource.Name,
                () => resourceStream,
                moduleResource.IsPublic), (object?)resourceStream);
        }
    }

    private static List<(ResourceDescription, object?)> GenerateManifestResources(string projectFilePath, string rootNamespace, IEnumerable<string> embeddedResources)
    {
        var resources = new List<(ResourceDescription, object?)>();
        var projectDir = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
        foreach (var embeddedResource in embeddedResources)
        {
            var resourceFullFilename = Path.Combine(projectDir, embeddedResource);

            var resourceName = GenerateResourceName(embeddedResource);

            resources.Add((new ResourceDescription(
                $"{rootNamespace}.{string.Join(".", resourceName.Split('\\'))}",
                () => ProvideResourceData(resourceFullFilename),
                true), null));
        }

        return resources;
    }

    private static Stream ProvideResourceData(string resourceFullFilename)
    {
        // For non-.resx files just create a FileStream object to read the file as binary data
        if (!resourceFullFilename.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(resourceFullFilename))
            {
                return new MemoryStream();
            }
            return File.OpenRead(resourceFullFilename);
        }

        var shortLivedBackingStream = new MemoryStream();
        using (var resourceWriter = new ResourceWriter(shortLivedBackingStream))
        {
            resourceWriter.TypeNameConverter = TypeNameConverter;
            using var resourceReader = new ResXResourceReader(resourceFullFilename);
            resourceReader.BasePath = Path.GetDirectoryName(resourceFullFilename);
            var dictionaryEnumerator = resourceReader.GetEnumerator();
            while (dictionaryEnumerator.MoveNext())
            {
                if (dictionaryEnumerator.Key is string resourceKey)
                {
                    resourceWriter.AddResource(resourceKey, dictionaryEnumerator.Value);
                }
            }
        }

        return new MemoryStream(shortLivedBackingStream.GetBuffer());
    }

    /// <summary>
    /// This is needed to fix a "Could not load file or assembly 'System.Drawing, Version=4.0.0.0"
    /// exception, although I'm not sure why that exception was occurring.
    /// </summary>
    private static string TypeNameConverter(Type objectType) =>
        objectType.AssemblyQualifiedName?.Replace("4.0.0.0", "2.0.0.0", StringComparison.Ordinal) ?? string.Empty;

    private static string GenerateResourceName(string filePath)
    {
        // Remove relative path sequences
        var resourceName = filePath.Replace("..\\", string.Empty, StringComparison.Ordinal);

        // If the resource is a resx file, take the file name and replace the extension with 'resources', otherwise return full resource name
        return resourceName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase) ?
                resourceName.Remove(0, 1 + resourceName.LastIndexOf('\\')).Replace(".resx", string.Empty, StringComparison.Ordinal) + ".resources" : filePath;
    }

    private static string? GetResourceDescriptionInternalName(ResourceDescription resource)
    {
        // Roslyn-internal API: ResourceDescription.ResourceName is private. Reflection is the only
        // way to read this field without forking Roslyn — accepting the surgical S3011 disable
        // for this single line, with rationale documented per CLAUDE.md.
#pragma warning disable S3011 // Roslyn-internal private field; alternative is forking Roslyn
        var field = typeof(ResourceDescription).GetField("ResourceName", BindingFlags.Instance | BindingFlags.NonPublic);
#pragma warning restore S3011
        return field?.GetValue(resource) as string;
    }
}
