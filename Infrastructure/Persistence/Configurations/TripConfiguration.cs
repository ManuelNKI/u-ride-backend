using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        // ──── Strings ────
        builder.Property(t => t.DriverUid).IsRequired().HasMaxLength(128);
        builder.Property(t => t.DriverName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.RouteName).IsRequired().HasMaxLength(250);
        builder.Property(t => t.OriginZone).IsRequired().HasMaxLength(150);
        builder.Property(t => t.DestinationZone).IsRequired().HasMaxLength(150);
        builder.Property(t => t.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Notes).HasMaxLength(500);

        // ──── Precio ────
        builder.Property(t => t.Price).HasPrecision(10, 2);

        // ──── Enum almacenado como string ────
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ──── ConfirmedPassengerUids — EF Core 8 JSON primitive collection ────
        builder.PrimitiveCollection(t => t.ConfirmedPassengerUids)
            .ElementType()
            .HasMaxLength(128);

        // ──── Owned Type: VehicleInfo ────
        builder.OwnsOne(t => t.Vehicle, v =>
        {
            v.Property(x => x.Plate).IsRequired().HasMaxLength(20).HasColumnName("Vehicle_Plate");
            v.Property(x => x.Model).IsRequired().HasMaxLength(100).HasColumnName("Vehicle_Model");
            v.Property(x => x.Brand).IsRequired().HasMaxLength(100).HasColumnName("Vehicle_Brand");
            v.Property(x => x.Color).IsRequired().HasMaxLength(50).HasColumnName("Vehicle_Color");
        });

        // ──── Owned Type: TripRules ────
        builder.OwnsOne(t => t.Rules, r =>
        {
            r.Property(x => x.Punctuality).HasColumnName("Rules_Punctuality");
            r.Property(x => x.Respect).HasColumnName("Rules_Respect");
            r.Property(x => x.NoSensitiveData).HasColumnName("Rules_NoSensitiveData");
        });

        // ──── FK: Driver ────
        builder.HasOne(t => t.Driver)
            .WithMany(u => u.DriverTrips)
            .HasForeignKey(t => t.DriverUid)
            .OnDelete(DeleteBehavior.Restrict);

        // ──── Índices ────
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DepartureAt);
        builder.HasIndex(t => new { t.OriginZone, t.DestinationZone });
    }
}
