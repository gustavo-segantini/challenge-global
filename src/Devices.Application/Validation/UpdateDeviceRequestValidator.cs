using Devices.Application.Contracts;
using FluentValidation;

namespace Devices.Application.Validation;

public sealed class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    public UpdateDeviceRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(request => request.Brand)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(request => request.State)
            .IsInEnum();
    }
}
