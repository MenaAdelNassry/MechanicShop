using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.RepairTasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class RepairTaskPartConfiguration : IEntityTypeConfiguration<RepairTaskPart>
{
    public void Configure(EntityTypeBuilder<RepairTaskPart> builder)
    {
        builder.ToTable("RepairTaskParts");

        // Shadow property for Foreign Key back to RepairTask
        builder.Property(p => p.RepairTaskId).IsRequired();

        // Composite Primary Key (RepairTaskId + InventoryItemId)
        builder.HasKey(p => new { p.RepairTaskId, p.InventoryItemId });

        builder.Property(p => p.InventoryItemId)
               .IsRequired();

        builder.Property(p => p.Quantity)
               .IsRequired();

        // Foreign Key Relationship to InventoryItem for DB integrity
        builder.HasOne<InventoryItem>()
               .WithMany()
               .HasForeignKey(p => p.InventoryItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.InventoryItemId);
    }
}