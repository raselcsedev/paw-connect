namespace PawConnect.Api.Domain.Entities;

public class PetPhoto
{
    public int Id { get; set; }

    public int PetId { get; set; }
    public Pet Pet { get; set; } = null!;

    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long SizeInBytes { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}