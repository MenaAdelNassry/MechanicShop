using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Employees;
using MechanicShop.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdentityUserId)
            .IsRequired(true);

        builder.HasIndex(e => e.IdentityUserId)
            .IsUnique()
            .HasDatabaseName("IX_Employees_IdentityUserId");

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<Employee>(e => e.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ComplexProperty(e => e.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.FirstName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("FirstName");

            nameBuilder.Property(n => n.LastName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("LastName");
        });

        builder.Property(e => e.PhoneNumber)
            .HasConversion(
                phone => phone.Value,
                value => PhoneNumber.Create(value).Value)
            .HasColumnName("PhoneNumber")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}