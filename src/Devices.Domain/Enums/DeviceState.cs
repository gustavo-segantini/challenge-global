using System.Text.Json.Serialization;

namespace Devices.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<DeviceState>))]
public enum DeviceState
{
    [JsonStringEnumMemberName("available")]
    Available,

    [JsonStringEnumMemberName("in-use")]
    InUse,

    [JsonStringEnumMemberName("inactive")]
    Inactive
}
