using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Infrastructure.Data.Configurations;

public class PetPhotoConfiguration : IEntityTypeConfiguration<PetPhoto>
{
    public void Configure(EntityTypeBuilder<PetPhoto> builder)
    {
        builder.ToTable("PetPhotos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StoredFileName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.ContentType).IsRequired().HasMaxLength(50);

        builder.HasIndex(p => p.StoredFileName).IsUnique();

        builder.HasOne(p => p.Pet)
            .WithMany(p => p.Photos)
            .HasForeignKey(p => p.PetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}