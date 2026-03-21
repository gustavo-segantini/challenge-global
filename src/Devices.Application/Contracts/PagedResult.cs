namespace Devices.Application.Contracts;

public sealed record PagedResult<T>(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);
