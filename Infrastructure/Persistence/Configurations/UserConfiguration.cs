using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.FirebaseUid);

        builder.Property(u => u.FirebaseUid)
            .HasMaxLength(128);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .HasMaxLength(200);

        builder.Property(u => u.Career).HasMaxLength(150);
        builder.Property(u => u.Zone).HasMaxLength(150);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.PhotoUrl).HasMaxLength(500);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
