using ExamPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPrep.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionName = "ExamPrep";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' is missing. Set ConnectionStrings__{ConnectionName} or use dotnet user-secrets.");

        services.AddDbContext<ExamPrepDbContext>(options => options.UseMySQL(connection));
        return services;
    }
}
