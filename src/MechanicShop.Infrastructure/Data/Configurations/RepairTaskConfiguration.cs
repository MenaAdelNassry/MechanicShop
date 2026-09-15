using MechanicShop.Domain.RepairTasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class RepairTaskConfiguration : IEntityTypeConfiguration<RepairTask>
{
    public void Configure(EntityTypeBuilder<RepairTask> builder)
    {
        builder.HasKey(rt => rt.Id).IsClustered(false);
        builder.Property(rt => rt.Id).ValueGeneratedNever();

        builder.HasIndex(rt => rt.CreatedAtUtc).IsClustered();

        builder.Property(rt => rt.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(rt => rt.EstimatedDurationInMins)
               .IsRequired();

        builder.Property(rt => rt.LaborCost)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        // Mapping One-To-Many relationship to RepairTaskPart
        builder.HasMany(rt => rt.Parts)
               .WithOne()
               .HasForeignKey("RepairTaskId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(rt => rt.Parts)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}