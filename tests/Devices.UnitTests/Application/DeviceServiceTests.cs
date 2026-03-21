using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Application.Contracts;
using Devices.Application.Services;
using Devices.Application.Validation;
using Devices.Domain.Entities;
using Devices.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Devices.UnitTests.Application;

public sealed class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _repository = new();

    [Fact]
    public async Task DeleteAsync_ShouldThrowConflict_WhenDeviceIsInUse()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Phone", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.DeleteAsync(id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ShouldThrowValidation_WhenNoFieldIsProvided()
    {
        var id = Guid.NewGuid();
        var service = CreateService();

        var result = await service.PatchAsync(id, new PatchDeviceRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("At least one field");
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowValidation_WhenPageSizeOutOfRange()
    {
        var service = CreateService();

        var result = await service.GetAllAsync(1, 0, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenDeviceDoesNotExist()
    {
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var service = CreateService();

        var result = await service.UpdateAsync(
            id,
            new UpdateDeviceRequest
            {
                Name = "Laptop",
                Brand = "Contoso",
                State = DeviceState.Available
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    private DeviceService CreateService()
    {
        return new DeviceService(
            _repository.Object,
            new CreateDeviceRequestValidator(),
            new UpdateDeviceRequestValidator(),
            new PatchDeviceRequestValidator());
    }
}
