using Devices.Domain.Entities;
using Devices.Domain.Enums;

namespace Devices.Application.Abstractions;

public interface IDeviceRepository
{
    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> GetByStateAsync(DeviceState state, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Device> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    void Remove(Device device);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
