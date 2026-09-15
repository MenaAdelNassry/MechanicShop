using MechanicShop.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicShop.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id).IsClustered(false);

        builder.HasIndex(rt => rt.CreatedAtUtc).IsClustered();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.ExpiresOnUtc).IsRequired();

        builder.Property(rt => rt.RevokedOnUtc).IsRequired(false);
    }
}