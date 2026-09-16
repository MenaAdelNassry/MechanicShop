using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Billing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");

        builder.HasKey(w => w.Id);

        builder.HasIndex(w => new { w.StartAtUtc, w.CreatedAtUtc });
        builder.HasIndex(w => w.TrackingToken).IsUnique();

        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.State)
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(w => w.SpotId).IsRequired();
        builder.HasOne(w => w.Spot)
            .WithMany()
            .HasForeignKey(w => w.SpotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(w => w.StartAtUtc).IsRequired();
        builder.Property(w => w.EndAtUtc).IsRequired();
        builder.Property(w => w.TrackingToken).IsRequired();

        builder.Ignore(w => w.Total);
        builder.Ignore(w => w.TotalLaborCost);
        builder.Ignore(w => w.TotalPartsCost);
        builder.Ignore(w => w.ActualDuration);
        builder.Ignore(w => w.IsEditable);

        builder.HasIndex(w => w.LaborId);
        builder.HasIndex(w => w.VehicleId);
        builder.HasIndex(w => w.State);

        builder.HasOne(w => w.Labor)
               .WithMany()
               .HasForeignKey(w => w.LaborId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Vehicle)
               .WithMany()
               .HasForeignKey(w => w.VehicleId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Invoice)
               .WithOne(i => i.WorkOrder)
               .HasForeignKey<Invoice>(i => i.WorkOrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.RepairTasks)
               .WithOne()
               .HasForeignKey(wt => wt.WorkOrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.RepairTasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}