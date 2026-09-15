using MechanicShop.Domain.Inventory;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(invItem => invItem.Id);
        builder.Property(invItem => invItem.Id).ValueGeneratedNever();

        builder.Property(invItem => invItem.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(invItem => invItem.Name)
               .IsUnique();

        builder.Property(invItem => invItem.Cost)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(invItem => invItem.StockQuantity)
               .IsRequired();

        builder.Property(invItem => invItem.ReorderLevel)
               .IsRequired();

        builder.Property(invItem => invItem.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // One-To-Many Relationship with Transactions using Backing Field _transactions
        builder.HasMany(invItem => invItem.Transactions)
               .WithOne()
               .HasForeignKey("InventoryItemId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(invItem => invItem.Transactions)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}