using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public class MyPetsQuery
{
    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus? Status { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;
}