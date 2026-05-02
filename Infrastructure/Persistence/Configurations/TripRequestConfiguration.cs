using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TripRequestConfiguration : IEntityTypeConfiguration<TripRequest>
{
    public void Configure(EntityTypeBuilder<TripRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.PassengerUid).IsRequired().HasMaxLength(128);
        builder.Property(r => r.PassengerName).IsRequired().HasMaxLength(200);

        // Enums como string
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        // FK: Trip
        builder.HasOne(r => r.Trip)
            .WithMany(t => t.Requests)
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK: Passenger → User
        builder.HasOne(r => r.Passenger)
            .WithMany(u => u.TripRequests)
            .HasForeignKey(r => r.PassengerUid)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice compuesto para evitar duplicados
        builder.HasIndex(r => new { r.TripId, r.PassengerUid }).IsUnique();
    }
}
