using Devices.Application.Abstractions;
using Devices.Application.Services;
using Devices.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateDeviceRequestValidator>();
        services.AddScoped<IDeviceService, DeviceService>();

        return services;
    }
}
