using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
        builder.Property(r => r.Details).HasMaxLength(1000);
        builder.Property(r => r.ResolutionNote).HasMaxLength(300);

        builder.HasOne(r => r.Pet)
            .WithMany()
            .HasForeignKey(r => r.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.PetId, r.ReporterId, r.OpenSlot }).IsUnique();
        builder.HasIndex(r => new { r.Status, r.CreatedAtUtc });
    }
}