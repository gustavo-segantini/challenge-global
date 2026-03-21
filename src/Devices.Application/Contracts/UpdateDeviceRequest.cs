using System.ComponentModel.DataAnnotations;
using Devices.Domain.Enums;

namespace Devices.Application.Contracts;

public sealed class UpdateDeviceRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Brand { get; init; } = string.Empty;

    public DeviceState State { get; init; }
}
