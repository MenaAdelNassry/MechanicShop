using MechanicShop.Domain.Workorders.Billing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.TransactionReference)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.ReceivedByUserId)
            .IsRequired(false);

        builder.Property(p => p.PaidAtUtc)
            .IsRequired(false);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid accidental deletion of payments when an invoice is deleted

        // Index
        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.PaidAtUtc);
        builder.HasIndex(p => new { p.Method, p.Status });
    }
}