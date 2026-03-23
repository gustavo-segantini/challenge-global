using System.Text.Json.Serialization;
using Asp.Versioning;
using Devices.Api.Middlewares;
using Devices.Api.Observability;
using Devices.Application.Abstractions;
using Devices.Application.DependencyInjection;
using Devices.Domain.Enums;
using Devices.Infrastructure.DependencyInjection;
using Devices.Infrastructure.Persistence;
using Devices.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ObservabilityOptions>(
    builder.Configuration.GetSection(ObservabilityOptions.SectionName));

var observabilityOptions = builder.Configuration
    .GetSection(ObservabilityOptions.SectionName)
    .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.ParseStateValues = true;

    if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(observabilityOptions.Otlp.Endpoint))
    {
        logging.AddOtlpExporter(exporterOptions =>
        {
            exporterOptions.Endpoint = new Uri(observabilityOptions.Otlp.Endpoint!);
        });
    }
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<DeviceState>()));

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("x-api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddApplication();
builder.Services.AddSingleton<HttpRequestBenchmarkCollector>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(observabilityOptions.ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(observabilityOptions.Otlp.Endpoint))
        {
            tracing.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(observabilityOptions.Otlp.Endpoint!);
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(HttpRequestBenchmarkCollector.MeterName);

        if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(observabilityOptions.Otlp.Endpoint))
        {
            metrics.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(observabilityOptions.Otlp.Endpoint!);
            });
        }
    });

if (builder.Environment.IsEnvironment("Testing"))
{
    var testingConnectionString = builder.Configuration.GetConnectionString("DevicesDbTest") ?? "Data Source=devices-tests.db";

    builder.Services.AddDbContext<DevicesDbContext>(options =>
        options.UseSqlite(testingConnectionString));
    builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
}
else
{
    builder.Services.AddInfrastructure(builder.Configuration);
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<DevicesDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new()
    {
        Title = "Devices API",
        Version = "v1",
        Description = "Versioned REST API for device lifecycle management."
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthorization();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DevicesDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }
}

app.MapControllers();
app.MapGet("/observability/benchmark", (HttpRequestBenchmarkCollector collector) => Results.Ok(collector.Snapshot()))
    .WithName("GetObservabilityBenchmark")
    .WithSummary("Returns simple benchmark metrics for HTTP request latency.")
    .WithDescription("Provides aggregate and recent-window latency statistics captured by middleware.");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program;
