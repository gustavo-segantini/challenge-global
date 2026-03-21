using Devices.Domain.Enums;

namespace Devices.Application.Contracts;

public sealed record DeviceResponse(
    Guid Id,
    string Name,
    string Brand,
    DeviceState State,
    DateTimeOffset CreationTime);
