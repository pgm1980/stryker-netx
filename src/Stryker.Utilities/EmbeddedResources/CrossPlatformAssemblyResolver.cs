using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Stryker.Utilities.EmbeddedResources;

// This (CrossPlatformAssemblyResolver) is a port of Mono.Cecil's BaseAssemblyResolver with all the
// conditional compilation removed and changes made to "Resolve". Modernized for stryker-netx
// (C# 14 / .NET 10): nullable annotations, file-scoped namespace, method-splits, ArgumentNullException.ThrowIfNull,
// CultureInfo.InvariantCulture, Array.Empty, StringComparer.Ordinal-overloads.
//
// Original: https://github.com/jbevain/cecil/blob/7b8ee049a151204997eecf587c69acc2f67c8405/Mono.Cecil/BaseAssemblyResolver.cs
// Author:   Jb Evain (jbevain@gmail.com)
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
// Licensed under the MIT/X11 license.
[ExcludeFromCodeCoverage]
public class CrossPlatformAssemblyResolver : IAssemblyResolver
{
    private static readonly bool _onMono = Type.GetType("Mono.Runtime") != null;
    private static readonly List<string> _directories = new(2) { ".", "bin" };

    // Maps file names of available trusted platform assemblies to their full paths.
    private static readonly Lazy<Dictionary<string, string>> TrustedPlatformAssemblies = new(CreateTrustedPlatformAssemblyMap);

    private List<string>? _gacPaths;

    // MA0046 surgical: AssemblyResolveEventHandler is a Mono.Cecil-defined delegate with a
    // non-void return type (returns AssemblyDefinition). The event signature is dictated by the
    // Mono.Cecil IAssemblyResolver contract — we cannot change it.
#pragma warning disable MA0046 // Mono.Cecil API contract — AssemblyResolveEventHandler returns AssemblyDefinition by design
    public event AssemblyResolveEventHandler? ResolveFailure;
#pragma warning restore MA0046

    public virtual AssemblyDefinition? Resolve(AssemblyNameReference name) => Resolve(name, new ReaderParameters());

    public virtual AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);

        var assembly = SearchDirectory(name, _directories, parameters);
        if (assembly != null)
        {
            return assembly;
        }

        if (name.IsRetargetable)
        {
            // if the reference is retargetable, zero it
            name = new AssemblyNameReference(name.Name, new Version(0, 0, 0, 0))
            {
                PublicKeyToken = Array.Empty<byte>(),
            };
        }

        // Try resolve as .NET core first (since stryker runs as .NET core, this is still the default)
        assembly = SearchTrustedPlatformAssemblies(name, parameters);
        if (assembly != null)
        {
            return assembly;
        }

        // If that fails, try as .NET framework
        assembly = TryResolveAsNetFramework(name, parameters);
        if (assembly != null)
        {
            return assembly;
        }

        if (ResolveFailure != null)
        {
            assembly = ResolveFailure(this, name);
            if (assembly != null)
            {
                return assembly;
            }
        }

        throw new AssemblyResolutionException(name);
    }

    private AssemblyDefinition? TryResolveAsNetFramework(AssemblyNameReference name, ReaderParameters parameters)
    {
        var frameworkDir = Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName);
        if (frameworkDir is null)
        {
            return null;
        }

        var frameworkDirs = _onMono
            ? new[] { frameworkDir, Path.Combine(frameworkDir, "Facades") }
            : new[] { frameworkDir };

        if (IsZero(name.Version))
        {
            var assembly = SearchDirectory(name, frameworkDirs, parameters);
            if (assembly != null)
            {
                return assembly;
            }
        }

        if (string.Equals(name.Name, "mscorlib", StringComparison.Ordinal))
        {
            var corlib = GetCorlib(name, parameters);
            if (corlib != null)
            {
                return corlib;
            }
        }

        var gacAssembly = GetAssemblyInGac(name, parameters);
        if (gacAssembly != null)
        {
            return gacAssembly;
        }

        return SearchDirectory(name, frameworkDirs, parameters);
    }

    private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        parameters.AssemblyResolver ??= this;
        return ModuleDefinition.ReadModule(file, parameters).Assembly;
    }

    private AssemblyDefinition? SearchTrustedPlatformAssemblies(AssemblyNameReference name, ReaderParameters parameters)
    {
        if (name.IsWindowsRuntime)
        {
            return null;
        }

        return TrustedPlatformAssemblies.Value.TryGetValue(name.Name, out var path) ? GetAssembly(path, parameters) : null;
    }

    private static Dictionary<string, string> CreateTrustedPlatformAssemblyMap()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? paths;

        try
        {
            paths = AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            paths = null;
        }

        if (paths == null)
        {
            return result;
        }

        foreach (var path in paths.Split(Path.PathSeparator)
            .Where(path => string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase)))
        {
            result[Path.GetFileNameWithoutExtension(path)] = path;
        }

        return result;
    }

    protected virtual AssemblyDefinition? SearchDirectory(AssemblyNameReference name,
        IEnumerable<string> directories, ReaderParameters parameters)
    {
        var extensions = name.IsWindowsRuntime ? new[] { ".winmd", ".dll" } : new[] { ".exe", ".dll" };
        foreach (var directory in directories)
        {
            foreach (var extension in extensions)
            {
                var file = Path.Combine(directory, name.Name + extension);
                if (!File.Exists(file))
                {
                    continue;
                }

                try
                {
                    return GetAssembly(file, parameters);
                }
                catch (BadImageFormatException ex)
                {
                    // Skip and try the next directory/extension combination — caller iterates further candidates.
                    System.Diagnostics.Debug.WriteLine($"Skipping non-loadable assembly '{file}': {ex.Message}");
                }
            }
        }

        return null;
    }

    private static bool IsZero(Version version) => version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0;

    private AssemblyDefinition? GetCorlib(AssemblyNameReference reference, ReaderParameters parameters)
    {
        var version = reference.Version;
        var corlib = typeof(object).Assembly.GetName();
        if (corlib.Version == version || IsZero(version))
        {
            return GetAssembly(typeof(object).Module.FullyQualifiedName, parameters);
        }

        var fqName = typeof(object).Module.FullyQualifiedName;
        var firstParent = Directory.GetParent(fqName);
        var secondParent = firstParent != null ? Directory.GetParent(firstParent.FullName) : null;
        if (secondParent is null)
        {
            return null;
        }

        var path = ResolveCorlibPath(secondParent.FullName, version);
        if (path is null)
        {
            return null;
        }

        var file = Path.Combine(path, "mscorlib.dll");
        if (File.Exists(file))
        {
            return GetAssembly(file, parameters);
        }

        if (_onMono && Directory.Exists(path + "-api"))
        {
            file = Path.Combine(path + "-api", "mscorlib.dll");
            if (File.Exists(file))
            {
                return GetAssembly(file, parameters);
            }
        }

        return null;
    }

    private static string? ResolveCorlibPath(string basePath, Version version) =>
        _onMono ? GetMonoCorlibPath(basePath, version) : GetNetFrameworkCorlibPath(basePath, version);

    private static string GetMonoCorlibPath(string basePath, Version version) => version.Major switch
    {
        1 => Path.Combine(basePath, "1.0"),
        2 => version.MajorRevision == 5 ? Path.Combine(basePath, "2.1") : Path.Combine(basePath, "2.0"),
        4 => Path.Combine(basePath, "4.0"),
        _ => throw new NotSupportedException("Version not supported: " + version),
    };

    private static string GetNetFrameworkCorlibPath(string basePath, Version version) => version.Major switch
    {
        1 => version.MajorRevision == 3300 ? Path.Combine(basePath, "v1.0.3705") : Path.Combine(basePath, "v1.1.4322"),
        2 => Path.Combine(basePath, "v2.0.50727"),
        4 => Path.Combine(basePath, "v4.0.30319"),
        _ => throw new NotSupportedException("Version not supported: " + version),
    };

    private static List<string> GetGacPaths()
    {
        if (_onMono)
        {
            return GetDefaultMonoGacPaths();
        }

        var paths = new List<string>(2);
        var windir = Environment.GetEnvironmentVariable("WINDIR");
        if (windir == null)
        {
            return paths;
        }

        paths.Add(Path.Combine(windir, "assembly"));
        paths.Add(Path.Combine(windir, Path.Combine("Microsoft.NET", "assembly")));
        return paths;
    }

    private static List<string> GetDefaultMonoGacPaths()
    {
        var paths = new List<string>(1);
        var gac = GetCurrentMonoGac();
        if (gac != null)
        {
            paths.Add(gac);
        }

        var gacPathsEnv = Environment.GetEnvironmentVariable("MONO_GAC_PREFIX");
        if (string.IsNullOrEmpty(gacPathsEnv))
        {
            return paths;
        }

        var prefixes = gacPathsEnv.Split(Path.PathSeparator);
        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            var gacPath = Path.Combine(Path.Combine(Path.Combine(prefix, "lib"), "mono"), "gac");
            if (Directory.Exists(gacPath) && (gac is null || !paths.Contains(gac, StringComparer.Ordinal)))
            {
                paths.Add(gacPath);
            }
        }

        return paths;
    }

    private static string? GetCurrentMonoGac()
    {
        var fqName = typeof(object).Module.FullyQualifiedName;
        var dir = Path.GetDirectoryName(fqName);
        if (dir is null)
        {
            return null;
        }

        var parent = Directory.GetParent(dir);
        return parent is null ? null : Path.Combine(parent.FullName, "gac");
    }

    private AssemblyDefinition? GetAssemblyInGac(AssemblyNameReference reference, ReaderParameters parameters)
    {
        if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
        {
            return null;
        }

        _gacPaths ??= GetGacPaths();

        return _onMono
            ? GetAssemblyInMonoGac(reference, parameters)
            : GetAssemblyInNetGac(reference, parameters);
    }

    private AssemblyDefinition? GetAssemblyInMonoGac(AssemblyNameReference reference, ReaderParameters parameters)
    {
        for (var i = 0; i < _gacPaths!.Count; i++)
        {
            var gacPath = _gacPaths[i];
            var file = GetAssemblyFile(reference, string.Empty, gacPath);
            if (File.Exists(file))
            {
                return GetAssembly(file, parameters);
            }
        }

        return null;
    }

    private AssemblyDefinition? GetAssemblyInNetGac(AssemblyNameReference reference, ReaderParameters parameters)
    {
        var gacs = new[] { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
        var prefixes = new[] { string.Empty, "v4.0_" };

        for (var i = 0; i < _gacPaths!.Count; i++)
        {
            for (var j = 0; j < gacs.Length; j++)
            {
                var gac = Path.Combine(_gacPaths[i], gacs[j]);
                var file = GetAssemblyFile(reference, prefixes[i], gac);
                if (Directory.Exists(gac) && File.Exists(file))
                {
                    return GetAssembly(file, parameters);
                }
            }
        }

        return null;
    }

    private static string GetAssemblyFile(AssemblyNameReference reference, string prefix, string gac)
    {
        var gacFolder = new StringBuilder()
            .Append(prefix)
            .Append(reference.Version)
            .Append("__");

        for (var i = 0; i < reference.PublicKeyToken.Length; i++)
        {
            gacFolder.Append(reference.PublicKeyToken[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return Path.Combine(
            Path.Combine(
                Path.Combine(gac, reference.Name), gacFolder.ToString()),
            reference.Name + ".dll");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
