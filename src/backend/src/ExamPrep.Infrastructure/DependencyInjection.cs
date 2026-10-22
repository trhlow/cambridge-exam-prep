using ExamPrep.Infrastructure.Content;
using ExamPrep.Infrastructure.Marking;
using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExamPrep.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionName = "ExamPrep";

    /// <summary>
    /// Connections in the pool unless the connection string sets its own size. MySql.Data's default (100) ran out at
    /// 200 candidates in the VE-41 load test: its async calls block, so a busy room holds many connections at once.
    /// MySQL itself must allow more (docker-compose.yml: --max-connections=300).
    /// </summary>
    public const int DefaultMaxPoolSize = 250;
    public const string AssetsRootSetting = "Storage:AssetsRoot";

    /// <param name="defaultAssetsRoot">Folder for uploaded pictures and audio when <c>Storage:AssetsRoot</c> is not set.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string defaultAssetsRoot)
    {
        var connection = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' is missing. Set ConnectionStrings__{ConnectionName} or use dotnet user-secrets.");

        services.AddDbContext<ExamPrepDbContext>(options => options.UseMySQL(WithPoolSize(connection)));

        var assetsRoot = configuration[AssetsRootSetting] ?? defaultAssetsRoot;
        services.AddSingleton<IAssetStorage>(new FileSystemAssetStorage(assetsRoot));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PaperPackageImporter>();
        services.AddScoped<PaperMixer>();
        services.AddScoped<AttemptMarker>();
        return services;
    }

    /// <summary>The connection string with <see cref="DefaultMaxPoolSize"/> added when it names no pool size.</summary>
    public static string WithPoolSize(string connection) =>
        connection.Contains("pool size", StringComparison.OrdinalIgnoreCase) || connection.Contains("maxpoolsize", StringComparison.OrdinalIgnoreCase)
            ? connection
            : $"{connection.TrimEnd(';')};Maximum Pool Size={DefaultMaxPoolSize}";
}
