using MechanicShop.Domain.Inventory;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.HasKey(invTran => invTran.Id);
        builder.Property(invTran => invTran.Id).ValueGeneratedNever();

        builder.Property(invTran => invTran.Type)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(invTran => invTran.Quantity)
               .IsRequired();

        builder.Property(invTran => invTran.Reason)
               .HasMaxLength(500);

        builder.Property(invTran => invTran.InventoryItemId).IsRequired();
    }
}