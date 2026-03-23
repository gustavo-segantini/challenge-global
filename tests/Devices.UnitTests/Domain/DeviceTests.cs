using Devices.Domain.Entities;
using Devices.Domain.Enums;
using Devices.Domain.Exceptions;
using FluentAssertions;

namespace Devices.UnitTests.Domain;

public sealed class DeviceTests
{
    [Fact]
    public void Create_ShouldThrow_WhenNameIsBlank()
    {
        var action = () => Device.Create("   ", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*name cannot be empty*");
    }

    [Fact]
    public void ApplyPatch_ShouldUpdateOnlyProvidedFields()
    {
        var device = Device.Create("Phone", "Contoso", DeviceState.Available, DateTimeOffset.UtcNow);

        device.ApplyPatch(null, "Fabrikam", null);

        device.Name.Should().Be("Phone");
        device.Brand.Should().Be("Fabrikam");
        device.State.Should().Be(DeviceState.Available);
    }

    [Fact]
    public void ApplyPatch_ShouldThrow_WhenStateIsInUseAndBrandChanges()
    {
        var device = Device.Create("Router", "Contoso", DeviceState.InUse, DateTimeOffset.UtcNow);

        var action = () => device.ApplyPatch(null, "Other", null);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*name and brand cannot be updated*");
    }

    [Fact]
    public void CreationTime_ShouldRemainImmutable_AfterUpdates()
    {
        var creationTime = DateTimeOffset.UtcNow;
        var device = Device.Create("Phone X", "Contoso", DeviceState.Available, creationTime);

        device.Replace("Phone X2", "Contoso", DeviceState.Inactive);
        device.ApplyPatch("Phone X3", null, null);

        device.CreationTime.Should().Be(creationTime);
    }

    [Fact]
    public void Replace_ShouldThrow_WhenStateIsInUseAndNameChanges()
    {
        var device = Device.Create("Router", "Fabrikam", DeviceState.InUse, DateTimeOffset.UtcNow);

        var action = () => device.Replace("Router Pro", "Fabrikam", DeviceState.InUse);

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*name and brand cannot be updated*");
    }

    [Fact]
    public void EnsureCanBeDeleted_ShouldThrow_WhenStateIsInUse()
    {
        var device = Device.Create("Tablet", "Northwind", DeviceState.InUse, DateTimeOffset.UtcNow);

        var action = device.EnsureCanBeDeleted;

        action.Should().Throw<DomainRuleException>()
            .WithMessage("*cannot be deleted*");
    }

    [Fact]
    public void EnsureCanBeDeleted_ShouldNotThrow_WhenStateIsNotInUse()
    {
        var device = Device.Create("Tablet", "Northwind", DeviceState.Available, DateTimeOffset.UtcNow);

        var action = device.EnsureCanBeDeleted;

        action.Should().NotThrow();
    }

    [Fact]
    public void Replace_ShouldAllowStateChange_WhenNameAndBrandStayEqualInInUseState()
    {
        var device = Device.Create("Scanner", "Tailspin", DeviceState.InUse, DateTimeOffset.UtcNow);

        device.Replace("Scanner", "Tailspin", DeviceState.Inactive);

        device.State.Should().Be(DeviceState.Inactive);
    }
}
