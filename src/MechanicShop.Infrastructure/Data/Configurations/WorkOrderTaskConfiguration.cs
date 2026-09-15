using MechanicShop.Domain.Workorders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class WorkOrderTaskConfiguration : IEntityTypeConfiguration<WorkOrderTask>
{
    public void Configure(EntityTypeBuilder<WorkOrderTask> builder)
    {
        builder.ToTable("WorkOrderTasks");

        builder.HasKey(wt => wt.Id);
        builder.Property(wt => wt.Id).ValueGeneratedNever();

        builder.Property(wt => wt.Name).IsRequired().HasMaxLength(100);
        builder.Property(wt => wt.LaborCost).HasColumnType("decimal(18,2)");
        builder.Property(wt => wt.EstimatedDurationInMins).IsRequired();

        builder.HasMany(wt => wt.Parts)
               .WithOne()
               .HasForeignKey(wp => wp.WorkOrderTaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(wt => wt.Parts)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(wt => wt.PartsCost);
        builder.Ignore(wt => wt.TotalCost);

        // When requesting a work order, including its tasks and parts.
        builder.HasIndex(wt => wt.WorkOrderId);
    }
}