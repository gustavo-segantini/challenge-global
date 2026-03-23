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
    public async Task GetById_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var response = await _client.GetAsync($"{BasePath}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenPayloadIsValid()
    {
        var created = await CreateDeviceAsync("Edge AP", "Contoso", DeviceState.Available);

        var response = await _client.PutAsJsonAsync(
            $"{BasePath}/{created.Id}",
            new UpdateDeviceRequest
            {
                Name = "Edge AP Pro",
                Brand = "Contoso",
                State = DeviceState.Inactive
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<DeviceResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Edge AP Pro");
        updated.State.Should().Be(DeviceState.Inactive);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenPayloadIsInvalid()
    {
        var created = await CreateDeviceAsync("AP", "Contoso", DeviceState.Available);

        var response = await _client.PutAsJsonAsync(
            $"{BasePath}/{created.Id}",
            new UpdateDeviceRequest
            {
                Name = string.Empty,
                Brand = "Contoso",
                State = DeviceState.Available
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BasePath}/{Guid.NewGuid()}",
            new UpdateDeviceRequest
            {
                Name = "Edge AP",
                Brand = "Contoso",
                State = DeviceState.Available
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task Patch_ShouldReturnOk_WhenOnlyStateChanges()
    {
        var created = await CreateDeviceAsync("Core Router", "Northwind", DeviceState.Available);

        var response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{created.Id}",
            new PatchDeviceRequest
            {
                State = DeviceState.Inactive
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<DeviceResponse>();
        updated.Should().NotBeNull();
        updated!.State.Should().Be(DeviceState.Inactive);
    }

    [Fact]
    public async Task Patch_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{Guid.NewGuid()}",
            new PatchDeviceRequest
            {
                State = DeviceState.Inactive
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task Delete_ShouldReturnNoContent_WhenDeviceIsNotInUse()
    {
        var created = await CreateDeviceAsync("Gateway", "Tailspin", DeviceState.Available);

        var response = await _client.DeleteAsync($"{BasePath}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var response = await _client.DeleteAsync($"{BasePath}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageNumberIsInvalid()
    {
        var response = await _client.GetAsync($"{BasePath}?pageNumber=0&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeIsTooLarge()
    {
        var response = await _client.GetAsync($"{BasePath}?pageNumber=1&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByBrand_ShouldReturnDevices_WhenBrandExists()
    {
        await CreateDeviceAsync("Laptop", "Adventure", DeviceState.Available);

        var response = await _client.GetAsync($"{BasePath}/brand/Adventure");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var devices = await response.Content.ReadFromJsonAsync<IReadOnlyList<DeviceResponse>>();
        devices.Should().NotBeNull();
        devices.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByState_ShouldReturnDevices_WhenStateIsValid()
    {
        await CreateDeviceAsync("Switch", "Wingtip", DeviceState.Inactive);

        var response = await _client.GetAsync($"{BasePath}/state/inactive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var devices = await response.Content.ReadFromJsonAsync<IReadOnlyList<DeviceResponse>>();
        devices.Should().NotBeNull();
        devices.Should().NotBeEmpty();
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
    public async Task AnyRequest_ShouldIncludeResponseTimeHeader()
    {
        var response = await _client.GetAsync("/health/live");

        response.Headers.Contains("X-Response-Time-Ms").Should().BeTrue();
    }

    [Fact]
    public async Task BenchmarkEndpoint_ShouldReturnSnapshot()
    {
        await _client.GetAsync("/health/live");
        await _client.GetAsync("/health/ready");

        var response = await _client.GetAsync("/observability/benchmark");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var snapshot = await response.Content.ReadFromJsonAsync<HttpRequestBenchmarkSnapshotResponse>();
        snapshot.Should().NotBeNull();
        snapshot!.TotalRequests.Should().BeGreaterThan(0);
        snapshot.AverageMs.Should().BeGreaterThanOrEqualTo(0);
        snapshot.MaxMs.Should().BeGreaterThanOrEqualTo(0);
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

    private async Task<DeviceResponse> CreateDeviceAsync(string name, string brand, DeviceState state)
    {
        var response = await _client.PostAsJsonAsync(
            BasePath,
            new CreateDeviceRequest
            {
                Name = name,
                Brand = brand,
                State = state
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<DeviceResponse>();
        created.Should().NotBeNull();

        return created!;
    }

    private sealed record HttpRequestBenchmarkSnapshotResponse(
        DateTimeOffset TimestampUtc,
        long TotalRequests,
        int WindowSize,
        double AverageMs,
        double MaxMs,
        double P95Ms);
}
