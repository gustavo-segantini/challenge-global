using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Devices.Api.Extensions;

public static class ControllerVersionExtensions
{
    public static string ResolveApiVersion(this ControllerBase controller, string fallbackVersion = "1.0")
    {
        return controller.HttpContext.GetRequestedApiVersion()?.ToString()
            ?? controller.RouteData.Values["version"]?.ToString()
            ?? fallbackVersion;
    }
}
