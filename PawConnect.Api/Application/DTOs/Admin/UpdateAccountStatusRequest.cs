using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Admin;

public class UpdateAccountStatusRequest
{
    [Required, EnumDataType(typeof(AccountStatus))]
    public AccountStatus? Status { get; set; }
}