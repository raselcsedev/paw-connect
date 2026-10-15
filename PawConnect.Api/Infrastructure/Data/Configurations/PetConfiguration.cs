using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Infrastructure.Data.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Breed).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.HealthStatus).HasMaxLength(500);
        builder.Property(p => p.SpecialCareRequirements).HasMaxLength(1000);
        builder.Property(p => p.Requirements).HasMaxLength(1000);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Area).HasMaxLength(100);

        builder.Property(p => p.Species).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Size).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Sex).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.AvailabilityType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Status);
    }
}