using Devices.Domain.Enums;
using Devices.Domain.Exceptions;

namespace Devices.Domain.Entities;

public sealed class Device
{
    private Device()
    {
    }

    private Device(Guid id, string name, string brand, DeviceState state, DateTimeOffset creationTime)
    {
        Id = id;
        Name = NormalizeRequired(name, nameof(name));
        Brand = NormalizeRequired(brand, nameof(brand));
        State = state;
        CreationTime = creationTime;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Brand { get; private set; } = string.Empty;

    public DeviceState State { get; private set; }

    public DateTimeOffset CreationTime { get; private set; }

    public static Device Create(string name, string brand, DeviceState state, DateTimeOffset creationTime)
    {
        return new Device(Guid.NewGuid(), name, brand, state, creationTime);
    }

    public void Replace(string name, string brand, DeviceState state)
    {
        var normalizedName = NormalizeRequired(name, nameof(name));
        var normalizedBrand = NormalizeRequired(brand, nameof(brand));

        EnsureCanChangeNameAndBrand(normalizedName, normalizedBrand);

        Name = normalizedName;
        Brand = normalizedBrand;
        State = state;
    }

    public void ApplyPatch(string? name, string? brand, DeviceState? state)
    {
        var normalizedName = name is null ? Name : NormalizeRequired(name, nameof(name));
        var normalizedBrand = brand is null ? Brand : NormalizeRequired(brand, nameof(brand));

        EnsureCanChangeNameAndBrand(normalizedName, normalizedBrand);

        if (name is not null)
        {
            Name = normalizedName;
        }

        if (brand is not null)
        {
            Brand = normalizedBrand;
        }

        if (state.HasValue)
        {
            State = state.Value;
        }
    }

    public void EnsureCanBeDeleted()
    {
        if (State == DeviceState.InUse)
        {
            throw new DomainRuleException("Devices in state 'in-use' cannot be deleted.");
        }
    }

    private void EnsureCanChangeNameAndBrand(string newName, string newBrand)
    {
        if (State != DeviceState.InUse)
        {
            return;
        }

        if (!string.Equals(Name, newName, StringComparison.Ordinal) ||
            !string.Equals(Brand, newBrand, StringComparison.Ordinal))
        {
            throw new DomainRuleException("When a device is in state 'in-use', name and brand cannot be updated.");
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainRuleException($"{parameterName} cannot be empty.");
        }

        return normalized;
    }
}
