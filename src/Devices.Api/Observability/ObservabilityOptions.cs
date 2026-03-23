namespace Devices.Api.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "devices-api";

    public int SlowRequestThresholdMs { get; set; } = 500;

    public OtlpOptions Otlp { get; set; } = new();

    public sealed class OtlpOptions
    {
        public string? Endpoint { get; set; }
    }
}