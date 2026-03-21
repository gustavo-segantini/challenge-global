using Devices.Application.Contracts;
using Devices.Domain.Entities;

namespace Devices.Application.Mappings;

public static class DeviceMappings
{
    public static DeviceResponse ToResponse(this Device device)
    {
        return new DeviceResponse(device.Id, device.Name, device.Brand, device.State, device.CreationTime);
    }
}
