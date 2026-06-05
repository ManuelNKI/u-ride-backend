using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Brand).IsRequired().HasMaxLength(50);
        builder.Property(v => v.ModelOrBusNumber).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Plate).IsRequired().HasMaxLength(20);
        builder.Property(v => v.Color).IsRequired().HasMaxLength(30);

        builder.HasOne(v => v.Owner)
            .WithMany(u => u.Vehicles)
            .HasForeignKey(v => v.OwnerUid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
