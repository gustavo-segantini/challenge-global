using Devices.Application.Contracts;
using Devices.Application.Common;

namespace Devices.Application.Abstractions;

public interface IDeviceService
{
    Task<Result<DeviceResponse>> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default);

    Task<Result<DeviceResponse>> UpdateAsync(Guid id, UpdateDeviceRequest request, CancellationToken cancellationToken = default);

    Task<Result<DeviceResponse>> PatchAsync(Guid id, PatchDeviceRequest request, CancellationToken cancellationToken = default);

    Task<Result<DeviceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<DeviceResponse>>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DeviceResponse>>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DeviceResponse>>> GetByStateAsync(string state, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
