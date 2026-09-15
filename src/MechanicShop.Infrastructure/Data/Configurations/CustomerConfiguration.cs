using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasMany(c => c.Vehicles)
            .WithOne(v => v.Customer)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ComplexProperty(c => c.Name, nameBuilder =>
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

        // Using Value Converter instead of ComplexProperty
        // ComplexProperty dosen't allow to set indexes on the properties
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value)
            .HasColumnName("Email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasConversion(
                phone => phone.Value,
                value => PhoneNumber.Create(value).Value)
            .HasColumnName("PhoneNumber")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");

        builder.HasIndex(c => c.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Customers_PhoneNumber");

        builder.Navigation(c => c.Vehicles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}