using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Devices.Infrastructure.Persistence.DesignTime;

public sealed class DevicesDbContextFactory : IDesignTimeDbContextFactory<DevicesDbContext>
{
    public DevicesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DEVICES_DB_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=devicesdb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<DevicesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DevicesDbContext(optionsBuilder.Options);
    }
}
