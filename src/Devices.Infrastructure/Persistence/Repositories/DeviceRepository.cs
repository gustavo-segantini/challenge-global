using Devices.Application.Abstractions;
using Devices.Domain.Entities;
using Devices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly DevicesDbContext _dbContext;

    public DeviceRepository(DevicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .SingleOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Where(device => device.Brand == brand)
            .OrderBy(device => device.CreationTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetByStateAsync(DeviceState state, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Where(device => device.State == state)
            .OrderBy(device => device.CreationTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Device> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Devices.OrderBy(device => device.CreationTime);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Remove(Device device)
    {
        _dbContext.Devices.Remove(device);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
