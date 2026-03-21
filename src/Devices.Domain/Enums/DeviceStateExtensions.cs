namespace Devices.Domain.Enums;

public static class DeviceStateExtensions
{
    public static string ToStorageValue(this DeviceState state)
    {
        return state switch
        {
            DeviceState.Available => "available",
            DeviceState.InUse => "in-use",
            DeviceState.Inactive => "inactive",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported device state.")
        };
    }

    public static DeviceState FromStorageValue(string value)
    {
        return Parse(value);
    }

    public static bool TryParse(string? value, out DeviceState state)
    {
        state = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "available":
                state = DeviceState.Available;
                return true;
            case "in-use":
                state = DeviceState.InUse;
                return true;
            case "inactive":
                state = DeviceState.Inactive;
                return true;
            default:
                return false;
        }
    }

    public static DeviceState Parse(string value)
    {
        if (TryParse(value, out var state))
        {
            return state;
        }

        throw new ArgumentException("State must be one of: available, in-use, inactive.", nameof(value));
    }
}
