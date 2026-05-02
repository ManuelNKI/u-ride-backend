using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.ReporterUid).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ReportedUid).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.EvidenceUrl).HasMaxLength(500);
        builder.Property(r => r.AdminNotes).HasMaxLength(1000);

        // Enums como string
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Action)
            .HasConversion<string>()
            .HasMaxLength(20);

        // FK: Reporter (Restrict)
        builder.HasOne(r => r.Reporter)
            .WithMany(u => u.ReportsFiled)
            .HasForeignKey(r => r.ReporterUid)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: ReportedUser (Restrict)
        builder.HasOne(r => r.ReportedUser)
            .WithMany(u => u.ReportsReceived)
            .HasForeignKey(r => r.ReportedUid)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Trip (opcional)
        builder.HasOne(r => r.Trip)
            .WithMany()
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
