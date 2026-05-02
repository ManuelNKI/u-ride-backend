using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.FromUid).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ToUid).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Stars).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);

        // FK: Trip
        builder.HasOne(r => r.Trip)
            .WithMany(t => t.Reviews)
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK: FromUser (Restrict para evitar cascada circular)
        builder.HasOne(r => r.FromUser)
            .WithMany(u => u.ReviewsGiven)
            .HasForeignKey(r => r.FromUid)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: ToUser (Restrict para evitar cascada circular)
        builder.HasOne(r => r.ToUser)
            .WithMany(u => u.ReviewsReceived)
            .HasForeignKey(r => r.ToUid)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
