using Devices.Application.Abstractions;
using Devices.Infrastructure.Persistence;
using Devices.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DevicesDb") ??
            configuration["DEVICES_DB_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string was not configured. Use ConnectionStrings:DevicesDb or DEVICES_DB_CONNECTION_STRING.");
        }

        services.AddDbContext<DevicesDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IDeviceRepository, DeviceRepository>();

        return services;
    }
}
