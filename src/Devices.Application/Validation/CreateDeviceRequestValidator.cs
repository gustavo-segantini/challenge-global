using Devices.Application.Contracts;
using FluentValidation;

namespace Devices.Application.Validation;

public sealed class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
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
