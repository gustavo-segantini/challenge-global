using Devices.Domain.Entities;
using Devices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

public sealed class DevicesDbContext : DbContext
{
    public DevicesDbContext(DbContextOptions<DevicesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(builder =>
        {
            builder.ToTable("devices");

            builder.HasKey(device => device.Id);

            builder.Property(device => device.Id)
                .HasColumnName("id");

            builder.Property(device => device.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(device => device.Brand)
                .HasColumnName("brand")
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(device => device.State)
                .HasColumnName("state")
                .HasConversion(
                    state => state.ToStorageValue(),
                    value => DeviceStateExtensions.FromStorageValue(value))
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(device => device.CreationTime)
                .HasColumnName("creation_time")
                .IsRequired();

            builder.HasIndex(device => device.Brand)
                .HasDatabaseName("ix_devices_brand");

            builder.HasIndex(device => device.State)
                .HasDatabaseName("ix_devices_state");
        });
    }
}
