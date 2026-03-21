using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Application.Contracts;
using Devices.Application.Mappings;
using Devices.Domain.Entities;
using Devices.Domain.Enums;
using Devices.Domain.Exceptions;
using FluentValidation;

namespace Devices.Application.Services;

public sealed class DeviceService : IDeviceService
{
    private const int MaxPageSize = 100;

    private readonly IDeviceRepository _repository;
    private readonly IValidator<CreateDeviceRequest> _createValidator;
    private readonly IValidator<UpdateDeviceRequest> _updateValidator;
    private readonly IValidator<PatchDeviceRequest> _patchValidator;

    public DeviceService(
        IDeviceRepository repository,
        IValidator<CreateDeviceRequest> createValidator,
        IValidator<UpdateDeviceRequest> updateValidator,
        IValidator<PatchDeviceRequest> patchValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _patchValidator = patchValidator;
    }

    public async Task<Result<DeviceResponse>> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateAsync(_createValidator, request, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(validationResult.Error!);
        }

        var device = Device.Create(request.Name, request.Brand, request.State, DateTimeOffset.UtcNow);

        await _repository.AddAsync(device, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DeviceResponse>.Success(device.ToResponse());
    }

    public async Task<Result<DeviceResponse>> UpdateAsync(Guid id, UpdateDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(validationResult.Error!);
        }

        var existingResult = await GetExistingDeviceAsync(id, cancellationToken);
        if (existingResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(existingResult.Error!);
        }

        var device = existingResult.Value!;

        var domainResult = ExecuteDomainRule(() => device.Replace(request.Name, request.Brand, request.State));
        if (domainResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(domainResult.Error!);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DeviceResponse>.Success(device.ToResponse());
    }

    public async Task<Result<DeviceResponse>> PatchAsync(Guid id, PatchDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateAsync(_patchValidator, request, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(validationResult.Error!);
        }

        var existingResult = await GetExistingDeviceAsync(id, cancellationToken);
        if (existingResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(existingResult.Error!);
        }

        var device = existingResult.Value!;

        var domainResult = ExecuteDomainRule(() => device.ApplyPatch(request.Name, request.Brand, request.State));
        if (domainResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(domainResult.Error!);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DeviceResponse>.Success(device.ToResponse());
    }

    public async Task<Result<DeviceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var device = await _repository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return Result<DeviceResponse>.Failure(NotFoundError($"Device '{id}' was not found."));
        }

        return Result<DeviceResponse>.Success(device.ToResponse());
    }

    public async Task<Result<PagedResult<DeviceResponse>>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
        {
            return Result<PagedResult<DeviceResponse>>.Failure(ValidationError("pageNumber must be greater than zero."));
        }

        if (pageSize <= 0 || pageSize > MaxPageSize)
        {
            return Result<PagedResult<DeviceResponse>>.Failure(ValidationError($"pageSize must be between 1 and {MaxPageSize}."));
        }

        var (items, totalCount) = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);

        var paged = new PagedResult<DeviceResponse>(
            pageNumber,
            pageSize,
            totalCount,
            items.Select(device => device.ToResponse()).ToArray());

        return Result<PagedResult<DeviceResponse>>.Success(paged);
    }

    public async Task<Result<IReadOnlyList<DeviceResponse>>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return Result<IReadOnlyList<DeviceResponse>>.Failure(ValidationError("brand cannot be empty."));
        }

        var devices = await _repository.GetByBrandAsync(brand.Trim(), cancellationToken);

        return Result<IReadOnlyList<DeviceResponse>>.Success(devices.Select(device => device.ToResponse()).ToArray());
    }

    public async Task<Result<IReadOnlyList<DeviceResponse>>> GetByStateAsync(string state, CancellationToken cancellationToken = default)
    {
        if (!DeviceStateExtensions.TryParse(state, out var parsedState))
        {
            return Result<IReadOnlyList<DeviceResponse>>.Failure(ValidationError("state must be one of: available, in-use, inactive."));
        }

        var devices = await _repository.GetByStateAsync(parsedState, cancellationToken);

        return Result<IReadOnlyList<DeviceResponse>>.Success(devices.Select(device => device.ToResponse()).ToArray());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existingResult = await GetExistingDeviceAsync(id, cancellationToken);
        if (existingResult.IsFailure)
        {
            return Result.Failure(existingResult.Error!);
        }

        var device = existingResult.Value!;

        var domainResult = ExecuteDomainRule(device.EnsureCanBeDeleted);
        if (domainResult.IsFailure)
        {
            return Result.Failure(domainResult.Error!);
        }

        _repository.Remove(device);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result<Device>> GetExistingDeviceAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await _repository.GetByIdAsync(id, cancellationToken);
        return device is null
            ? Result<Device>.Failure(NotFoundError($"Device '{id}' was not found."))
            : Result<Device>.Success(device);
    }

    private static Result ExecuteDomainRule(Action action)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (DomainRuleException exception)
        {
            return Result.Failure(ConflictError(exception.Message));
        }
    }

    private static async Task<Result> ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(instance, cancellationToken);
        if (validationResult.IsValid)
        {
            return Result.Success();
        }

        var combinedErrors = string.Join(
            " | ",
            validationResult.Errors
                .Select(error => error.ErrorMessage)
                .Distinct());

        return Result.Failure(ValidationError(combinedErrors));
    }

    private static Error ValidationError(string message)
    {
        return new Error("validation_error", message, ErrorType.Validation);
    }

    private static Error NotFoundError(string message)
    {
        return new Error("resource_not_found", message, ErrorType.NotFound);
    }

    private static Error ConflictError(string message)
    {
        return new Error("business_rule_conflict", message, ErrorType.Conflict);
    }
}
