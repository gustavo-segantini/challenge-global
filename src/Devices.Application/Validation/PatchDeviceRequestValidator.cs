using Devices.Application.Contracts;
using FluentValidation;

namespace Devices.Application.Validation;

public sealed class PatchDeviceRequestValidator : AbstractValidator<PatchDeviceRequest>
{
    public PatchDeviceRequestValidator()
    {
        RuleFor(request => request.Name)
            .MaximumLength(120)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name cannot be empty when provided.");

        RuleFor(request => request.Brand)
            .MaximumLength(120)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Brand cannot be empty when provided.");

        RuleFor(request => request.State)
            .IsInEnum()
            .When(request => request.State.HasValue);

        RuleFor(request => request)
            .Must(request => request.Name is not null || request.Brand is not null || request.State.HasValue)
            .WithMessage("At least one field must be provided for patch updates.");
    }
}
