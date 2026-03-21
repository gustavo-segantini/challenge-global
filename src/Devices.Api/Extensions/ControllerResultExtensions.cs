using Devices.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Devices.Api.Extensions;

public static class ControllerResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        Func<T, ActionResult<T>> onSuccess)
    {
        if (result.IsFailure)
        {
            return new ActionResult<T>(controller.ToProblemResult(result.Error!));
        }

        return onSuccess(result.Value!);
    }

    public static IActionResult ToActionResult(
        this ControllerBase controller,
        Result result,
        Func<IActionResult> onSuccess)
    {
        if (result.IsFailure)
        {
            return controller.ToProblemResult(result.Error!);
        }

        return onSuccess();
    }

    public static ObjectResult ToProblemResult(this ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problemDetails.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        problemDetails.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }
}
