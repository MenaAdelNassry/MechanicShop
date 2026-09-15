using MechanicShop.Domain.Workorders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class WorkOrderTaskPartConfiguration : IEntityTypeConfiguration<WorkOrderTaskPart>
{
    public void Configure(EntityTypeBuilder<WorkOrderTaskPart> builder)
    {
        builder.ToTable("WorkOrderTaskParts");

        builder.HasKey(wp => wp.Id);
        builder.Property(wp => wp.Id).ValueGeneratedNever();

        builder.Property(wp => wp.Name).IsRequired().HasMaxLength(100);
        builder.Property(wp => wp.Cost).HasColumnType("decimal(18,2)");
        builder.Property(wp => wp.Quantity).IsRequired();

        // Indexes for foreign keys
        // When requesting a work order, including its tasks and parts.
        // High Speed for Cascade Delete
        builder.HasIndex(wp => wp.WorkOrderTaskId);

        // Useful for Analytical Queries and Reports
        // Like: How many times was this part actually consumed in work orders over the past year?
        builder.HasIndex(wp => wp.InventoryItemId);
    }
}