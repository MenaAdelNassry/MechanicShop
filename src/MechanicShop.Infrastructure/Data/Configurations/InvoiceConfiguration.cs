using MechanicShop.Domain.Workorders.Billing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.HasIndex(i => i.WorkOrderId).IsUnique();

        builder.Property(i => i.IssuedAtUtc)
            .IsRequired();

        builder.Property(i => i.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(i => i.TaxRateAtIssuance)
               .HasPrecision(5, 4)
               .IsRequired();

        builder.Property(i => i.PaidAt);

        builder.Ignore(i => i.TaxAmount);

        builder.Navigation(i => i.LineItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.LineItems, items =>
        {
            items.ToTable("InvoiceLineItems");

            items.WithOwner().HasForeignKey(i => i.InvoiceId);

            items.HasKey(i => new { i.InvoiceId, i.LineNumber });

            items.Property(i => i.LineNumber)
            .ValueGeneratedNever();

            items.Property(i => i.Description)
                .HasMaxLength(200)
                .IsRequired();

            items.Property(i => i.Quantity)
                .IsRequired();

            items.Property(i => i.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.Navigation(i => i.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
    }
}