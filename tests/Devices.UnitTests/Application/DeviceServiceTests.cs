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
    public async Task CreateAsync_ShouldReturnValidation_WhenRequestIsInvalid()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            new CreateDeviceRequest { Name = "", Brand = "Contoso", State = DeviceState.Available },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAndReturnDevice_WhenRequestIsValid()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            new CreateDeviceRequest { Name = "Phone", Brand = "Contoso", State = DeviceState.Available },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        _repository.Verify(repository => repository.AddAsync(It.IsAny<Device>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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
    public async Task PatchAsync_ShouldReturnNotFound_WhenDeviceDoesNotExist()
    {
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var service = CreateService();

        var result = await service.PatchAsync(
            id,
            new PatchDeviceRequest { State = DeviceState.Available },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task PatchAsync_ShouldReturnConflict_WhenInUseDeviceChangesName()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Router", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.PatchAsync(
            id,
            new PatchDeviceRequest { Name = "Router Pro" },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task PatchAsync_ShouldReturnSuccess_WhenStateOnlyIsPatched()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Router", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.PatchAsync(
            id,
            new PatchDeviceRequest { State = DeviceState.Inactive },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(DeviceState.Inactive);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetAllAsync_ShouldThrowValidation_WhenPageNumberIsInvalid()
    {
        var service = CreateService();

        var result = await service.GetAllAsync(0, 10, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowValidation_WhenPageSizeExceedsMax()
    {
        var service = CreateService();

        var result = await service.GetAllAsync(1, 101, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult_WhenInputIsValid()
    {
        var service = CreateService();
        var devices = new[]
        {
            Device.Create("A", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow),
            Device.Create("B", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow)
        };

        _repository
            .Setup(repository => repository.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((devices, devices.Length));

        var result = await service.GetAllAsync(1, 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
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

    [Fact]
    public async Task UpdateAsync_ShouldReturnValidation_WhenRequestIsInvalid()
    {
        var service = CreateService();

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateDeviceRequest
            {
                Name = "",
                Brand = "Contoso",
                State = DeviceState.Available
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnConflict_WhenInUseDeviceChangesBrand()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Laptop", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.UpdateAsync(
            id,
            new UpdateDeviceRequest
            {
                Name = "Laptop",
                Brand = "Other",
                State = DeviceState.InUse
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenValidUpdate()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Laptop", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.UpdateAsync(
            id,
            new UpdateDeviceRequest
            {
                Name = "Laptop Pro",
                Brand = "Contoso",
                State = DeviceState.Inactive
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop Pro");
        result.Value.State.Should().Be(DeviceState.Inactive);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenDeviceDoesNotExist()
    {
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var service = CreateService();

        var result = await service.GetByIdAsync(id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDevice_WhenItExists()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Tablet", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.GetByIdAsync(id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByBrandAsync_ShouldReturnValidation_WhenBrandIsBlank()
    {
        var service = CreateService();

        var result = await service.GetByBrandAsync("   ", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetByBrandAsync_ShouldTrimInputAndReturnResults()
    {
        var device = Device.Create("Tablet", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByBrandAsync("Contoso", It.IsAny<CancellationToken>()))
            .ReturnsAsync([device]);

        var service = CreateService();

        var result = await service.GetByBrandAsync("  Contoso  ", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStateAsync_ShouldReturnValidation_WhenStateIsInvalid()
    {
        var service = CreateService();

        var result = await service.GetByStateAsync("invalid", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetByStateAsync_ShouldReturnResults_WhenStateIsValid()
    {
        var device = Device.Create("Tablet", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByStateAsync(DeviceState.InUse, It.IsAny<CancellationToken>()))
            .ReturnsAsync([device]);

        var service = CreateService();

        var result = await service.GetByStateAsync("in-use", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenDeviceDoesNotExist()
    {
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var service = CreateService();

        var result = await service.DeleteAsync(id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAndSave_WhenDeviceCanBeDeleted()
    {
        var id = Guid.NewGuid();
        var device = Device.Create("Phone", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var service = CreateService();

        var result = await service.DeleteAsync(id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.Remove(device), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
