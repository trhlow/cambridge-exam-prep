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
    public const string AssetsRootSetting = "Storage:AssetsRoot";

    /// <param name="defaultAssetsRoot">Folder for uploaded pictures and audio when <c>Storage:AssetsRoot</c> is not set.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string defaultAssetsRoot)
    {
        var connection = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' is missing. Set ConnectionStrings__{ConnectionName} or use dotnet user-secrets.");

        services.AddDbContext<ExamPrepDbContext>(options => options.UseMySQL(connection));

        var assetsRoot = configuration[AssetsRootSetting] ?? defaultAssetsRoot;
        services.AddSingleton<IAssetStorage>(new FileSystemAssetStorage(assetsRoot));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PaperPackageImporter>();
        services.AddScoped<AttemptMarker>();
        return services;
    }
}
