using Devices.Domain.Enums;
using FluentAssertions;

namespace Devices.UnitTests.Domain;

public sealed class DeviceStateExtensionsTests
{
    [Theory]
    [InlineData(DeviceState.Available, "available")]
    [InlineData(DeviceState.InUse, "in-use")]
    [InlineData(DeviceState.Inactive, "inactive")]
    public void ToStorageValue_ShouldMapKnownStates(DeviceState state, string expected)
    {
        state.ToStorageValue().Should().Be(expected);
    }

    [Fact]
    public void ToStorageValue_ShouldThrow_WhenStateIsUnsupported()
    {
        var state = (DeviceState)999;

        var action = () => state.ToStorageValue();

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_ShouldReturnFalse_ForBlankValues(string? input)
    {
        var result = DeviceStateExtensions.TryParse(input, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default);
    }

    [Theory]
    [InlineData("available", DeviceState.Available)]
    [InlineData("in-use", DeviceState.InUse)]
    [InlineData("inactive", DeviceState.Inactive)]
    [InlineData("  In-Use  ", DeviceState.InUse)]
    public void TryParse_ShouldReturnTrue_ForValidValues(string input, DeviceState expected)
    {
        var result = DeviceStateExtensions.TryParse(input, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Fact]
    public void Parse_ShouldThrow_ForInvalidValue()
    {
        var action = () => DeviceStateExtensions.Parse("invalid");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromStorageValue_ShouldReturnParsedState()
    {
        var state = DeviceStateExtensions.FromStorageValue("available");

        state.Should().Be(DeviceState.Available);
    }
}
