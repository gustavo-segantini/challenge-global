using Asp.Versioning;
using Devices.Application.Abstractions;
using Devices.Application.Contracts;
using Devices.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Devices.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/devices")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new device",
        Description = "Creates a device with name, brand, and state. CreationTime is set by the server and is immutable.")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> CreateDevice([FromBody] CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _deviceService.CreateAsync(request, cancellationToken);
        var requestedVersion = this.ResolveApiVersion();

        return this.ToActionResult(
            result,
            createdDevice => CreatedAtAction(nameof(GetDeviceById), new { version = requestedVersion, id = createdDevice.Id }, createdDevice));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Fully update a device",
        Description = "Replaces all mutable fields of a device. CreationTime cannot be changed.")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> UpdateDevice(Guid id, [FromBody] UpdateDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _deviceService.UpdateAsync(id, request, cancellationToken);

        return this.ToActionResult(result, updatedDevice => Ok(updatedDevice));
    }

    [HttpPatch("{id:guid}")]
    [SwaggerOperation(
        Summary = "Partially update a device",
        Description = "Updates only provided mutable fields. Name and brand updates are blocked when device state is in-use.")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> PatchDevice(Guid id, [FromBody] PatchDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _deviceService.PatchAsync(id, request, cancellationToken);

        return this.ToActionResult(result, patchedDevice => Ok(patchedDevice));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Fetch a single device by id",
        Description = "Returns a device for the provided identifier.")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> GetDeviceById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deviceService.GetByIdAsync(id, cancellationToken);

        return this.ToActionResult(result, device => Ok(device));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Fetch all devices",
        Description = "Returns paginated device results.")]
    [ProducesResponseType(typeof(PagedResult<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DeviceResponse>>> GetAllDevices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _deviceService.GetAllAsync(pageNumber, pageSize, cancellationToken);

        return this.ToActionResult(result, value => Ok(value));
    }

    [HttpGet("brand/{brand}")]
    [SwaggerOperation(
        Summary = "Fetch devices by brand",
        Description = "Returns all devices with an exact brand match.")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> GetDevicesByBrand(string brand, CancellationToken cancellationToken)
    {
        var result = await _deviceService.GetByBrandAsync(brand, cancellationToken);

        return this.ToActionResult(result, value => Ok(value));
    }

    [HttpGet("state/{state}")]
    [SwaggerOperation(
        Summary = "Fetch devices by state",
        Description = "Returns all devices for a state value: available, in-use, inactive.")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> GetDevicesByState(string state, CancellationToken cancellationToken)
    {
        var result = await _deviceService.GetByStateAsync(state, cancellationToken);

        return this.ToActionResult(result, value => Ok(value));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete a device",
        Description = "Deletes a device by identifier. Devices in state in-use cannot be deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deviceService.DeleteAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }
}
