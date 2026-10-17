using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Infrastructure.Data.Configurations;

public class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> builder)
    {
        builder.ToTable("Applications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.LivingArrangement).HasConversion<string>().HasMaxLength(20);

        builder.Property(a => a.HouseholdInfo).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.OtherPetsInfo).HasMaxLength(1000);
        builder.Property(a => a.PreviousExperience).HasMaxLength(1000);
        builder.Property(a => a.CarePlan).IsRequired().HasMaxLength(1500);
        builder.Property(a => a.ReasonForApplying).IsRequired().HasMaxLength(1500);
        builder.Property(a => a.AdditionalNotes).HasMaxLength(1000);

        builder.HasOne(a => a.Pet)
            .WithMany()
            .HasForeignKey(a => a.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Applicant)
            .WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PetId, a.ApplicantId, a.ActiveSlot }).IsUnique();
        builder.HasIndex(a => new { a.ApplicantId, a.Status });
        builder.HasIndex(a => new { a.PetId, a.Status });
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasIndex(a => new { a.PetId, a.ApprovedAdoptionSlot }).IsUnique();
    }
}