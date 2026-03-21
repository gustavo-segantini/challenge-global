using System.ComponentModel.DataAnnotations;
using Devices.Domain.Enums;

namespace Devices.Application.Contracts;

public sealed class PatchDeviceRequest
{
    [MaxLength(120)]
    public string? Name { get; init; }

    [MaxLength(120)]
    public string? Brand { get; init; }

    public DeviceState? State { get; init; }
}
