using MechanicShop.Domain.Customers.Vehicles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Make)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(v => v.Model)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(v => v.Year).IsRequired();

        builder.Property(v => v.LicensePlate)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(v => v.CustomerId).IsRequired();

        // Indexes
        builder.HasIndex(v => v.LicensePlate).IsUnique();
        builder.HasIndex(v => v.CustomerId);
    }
}