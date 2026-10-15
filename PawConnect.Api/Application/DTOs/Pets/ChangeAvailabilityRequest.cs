using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public class ChangeAvailabilityRequest
{
    [Required, EnumDataType(typeof(ListingStatus))]
    public ListingStatus? Status { get; set; }
}