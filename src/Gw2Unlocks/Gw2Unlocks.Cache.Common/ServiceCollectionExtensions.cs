using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Gw2Unlocks.Cache.Common;

public class CachePaths(string? baseDirectory = null)
{
    public string SolutionDir { get; } = baseDirectory ?? FindDirectoryBuildProps(AppContext.BaseDirectory) ?? throw new FileNotFoundException("Directory.Build.props not found.");

    public string CacheDir => Path.Combine(SolutionDir, "../cache-root");

    public string SiteDir => Path.Combine(SolutionDir, "./site");

    private static string? FindDirectoryBuildProps(string? directory)
    {
        while (!string.IsNullOrEmpty(directory))
        {
            string filePath = Path.Combine(directory, "Directory.Build.props");
            if (File.Exists(filePath))
                return directory;

            directory = Path.GetDirectoryName(directory);
        }
        return null;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCacheDir(
        this IServiceCollection services,
        Func<string?>? baseDirFactory = null)
    {
        services.AddSingleton(sp =>
        {
            string? baseDir = baseDirFactory?.Invoke();
            return new CachePaths(baseDir);
        });
        return services;
    }
}