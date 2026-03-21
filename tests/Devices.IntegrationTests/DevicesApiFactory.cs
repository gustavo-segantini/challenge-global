using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Devices.IntegrationTests;

public sealed class DevicesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"devices-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DevicesDbTest"] = $"Data Source={_databasePath}"
            });
        });
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public new Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }
}
