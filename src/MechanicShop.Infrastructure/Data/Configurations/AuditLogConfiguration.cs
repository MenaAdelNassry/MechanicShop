using MechanicShop.Domain.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TableName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Type).IsRequired().HasMaxLength(20);
        builder.Property(a => a.PrimaryKey).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UserId).HasMaxLength(450);

        // JSON Columns (nvarchar(max))
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.AffectedColumns).HasColumnType("nvarchar(max)");

        // Composite Indexes
        builder.HasIndex(a => new { a.TableName, a.PrimaryKey });
        builder.HasIndex(a => a.DateTimeUtc);
        builder.HasIndex(a => a.UserId);
    }
}