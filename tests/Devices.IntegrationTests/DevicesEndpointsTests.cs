using System.Net;
using System.Net.Http.Json;
using Devices.Application.Contracts;
using Devices.Domain.Enums;
using FluentAssertions;

namespace Devices.IntegrationTests;

public sealed class DevicesEndpointsTests : IClassFixture<DevicesApiFactory>
{
    private const string BasePath = "/api/v1/devices";

    private readonly HttpClient _client;

    public DevicesEndpointsTests(DevicesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndGetById_ShouldReturnCreatedDevice()
    {
        var createRequest = new CreateDeviceRequest
        {
            Name = "Edge Router",
            Brand = "Fabrikam",
            State = DeviceState.Available
        };

        var createResponse = await _client.PostAsJsonAsync(BasePath, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdDevice = await createResponse.Content.ReadFromJsonAsync<DeviceResponse>();
        createdDevice.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"{BasePath}/{createdDevice!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedDevice = await getResponse.Content.ReadFromJsonAsync<DeviceResponse>();
        fetchedDevice.Should().NotBeNull();
        fetchedDevice!.Name.Should().Be("Edge Router");
        fetchedDevice.Brand.Should().Be("Fabrikam");
        fetchedDevice.State.Should().Be(DeviceState.Available);
    }

    [Fact]
    public async Task Patch_ShouldReturnConflict_WhenInUseDeviceChangesName()
    {
        var createResponse = await _client.PostAsJsonAsync(
            BasePath,
            new CreateDeviceRequest
            {
                Name = "Core Switch",
                Brand = "Northwind",
                State = DeviceState.InUse
            });

        var createdDevice = await createResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        var patchResponse = await _client.PatchAsJsonAsync(
            $"{BasePath}/{createdDevice!.Id}",
            new PatchDeviceRequest
            {
                Name = "Core Switch v2"
            });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_ShouldReturnConflict_WhenDeviceIsInUse()
    {
        var createResponse = await _client.PostAsJsonAsync(
            BasePath,
            new CreateDeviceRequest
            {
                Name = "Gateway",
                Brand = "Tailspin",
                State = DeviceState.InUse
            });

        var createdDevice = await createResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        var deleteResponse = await _client.DeleteAsync($"{BasePath}/{createdDevice!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetByState_ShouldReturnBadRequest_ForInvalidState()
    {
        var response = await _client.GetAsync($"{BasePath}/state/invalid-state");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthReady_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenPayloadIsInvalid()
    {
        var response = await _client.PostAsJsonAsync(
            BasePath,
            new CreateDeviceRequest
            {
                Name = string.Empty,
                Brand = " ",
                State = DeviceState.Available
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
