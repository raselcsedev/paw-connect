using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public int? ApplicationId { get; set; }
    public AdoptionApplication? Application { get; set; }

    public int? PetId { get; set; }
    public Pet? Pet { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}