using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        builder.Property(n => n.UserUid).IsRequired().HasMaxLength(128);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.DriverUid).HasMaxLength(128);
        builder.Property(n => n.DriverName).HasMaxLength(200);

        // Enum como string
        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        // FK: User (destinatario)
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserUid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.UserUid);
        builder.HasIndex(n => new { n.UserUid, n.Read });
    }
}
