using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Tests;

public class DtoValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool Invalid(object model, string member) =>
        Validate(model).Any(r => r.MemberNames.Contains(member));

    /* ---------- registration ---------- */

    private static RegisterRequest Register(Action<RegisterRequest>? change = null)
    {
        var request = new RegisterRequest
        {
            Email = "sam@example.com",
            Password = "Rescue#Paws2026",
            DisplayName = "Sam",
            Role = "Adopter",
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public void Register_ValidRequest_HasNoErrors() => Assert.Empty(Validate(Register()));

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("adopter")]
    [InlineData("Rescuer ")]
    [InlineData("Adopter,Admin")]
    [InlineData("")]
    public void Register_AnythingButAdopterOrRescuer_IsRejected_SoNobodyCanSelfAssignAdmin(string role)
    {
        Assert.True(Invalid(Register(r => r.Role = role), nameof(RegisterRequest.Role)));
    }

    [Theory]
    [InlineData("Adopter")]
    [InlineData("Rescuer")]
    public void Register_AllowedRoles_AreAccepted(string role)
    {
        Assert.False(Invalid(Register(r => r.Role = role), nameof(RegisterRequest.Role)));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    public void Register_ShortPassword_IsRejected(string password)
    {
        Assert.True(Invalid(Register(r => r.Password = password), nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void Register_HugePassword_IsRejected_SoHashingCannotBeUsedToWasteServerTime()
    {
        Assert.True(Invalid(Register(r => r.Password = new string('a', 129)), nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void Register_BadEmail_IsRejected()
    {
        Assert.True(Invalid(Register(r => r.Email = "not-an-email"), nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void Login_EmptyFields_AreRejected()
    {
        var results = Validate(new LoginRequest());

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Email)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginRequest.Password)));
    }

    [Fact]
    public void RefreshToken_TooShort_IsRejected()
    {
        Assert.True(Invalid(new RefreshTokenRequest { RefreshToken = "abc" }, nameof(RefreshTokenRequest.RefreshToken)));
    }

    /* ---------- pets ---------- */

    private static CreatePetRequest Pet(Action<CreatePetRequest>? change = null)
    {
        var request = new CreatePetRequest
        {
            Name = "Bella",
            Species = Species.Rabbit,
            Size = PetSize.Small,
            AgeInMonths = 24,
            AvailabilityType = AvailabilityType.Both,
            City = "Dhaka",
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public void CreatePet_ValidRequest_HasNoErrors() => Assert.Empty(Validate(Pet()));

    [Fact]
    public void CreatePet_MissingSpecies_IsRejected_BecauseItIsNullableOnPurpose()
    {
        Assert.True(Invalid(Pet(p => p.Species = null), nameof(CreatePetRequest.Species)));
    }

    [Fact]
    public void CreatePet_MissingAvailabilityType_IsRejected()
    {
        Assert.True(Invalid(Pet(p => p.AvailabilityType = null), nameof(CreatePetRequest.AvailabilityType)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(360, false)]
    [InlineData(361, true)]
    public void CreatePet_AgeIsLimitedToThirtyYears(int months, bool invalid)
    {
        Assert.Equal(invalid, Invalid(Pet(p => p.AgeInMonths = months), nameof(CreatePetRequest.AgeInMonths)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void CreatePet_ShortName_IsRejected(string name)
    {
        Assert.True(Invalid(Pet(p => p.Name = name), nameof(CreatePetRequest.Name)));
    }

    /* ---------- search ---------- */

    [Fact]
    public void PetQuery_Defaults_AreValid() => Assert.Empty(Validate(new PetQueryParameters()));

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void PetQuery_PageAndPageSize_AreBounded(int page, int pageSize)
    {
        Assert.NotEmpty(Validate(new PetQueryParameters { Page = page, PageSize = pageSize }));
    }

    [Fact]
    public void PetQuery_MinAgeAboveMaxAge_IsRejected()
    {
        var results = Validate(new PetQueryParameters { MinAgeMonths = 50, MaxAgeMonths = 10 });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PetQueryParameters.MinAgeMonths)));
    }

    [Fact]
    public void PetQuery_EqualMinAndMaxAge_IsFine()
    {
        Assert.Empty(Validate(new PetQueryParameters { MinAgeMonths = 12, MaxAgeMonths = 12 }));
    }

    /* ---------- applications ---------- */

    private static CreateApplicationRequest Application(Action<CreateApplicationRequest>? change = null)
    {
        var request = new CreateApplicationRequest
        {
            PetId = 1,
            Type = ApplicationType.Adoption,
            LivingArrangement = LivingArrangement.House,
            HouseholdInfo = "Two adults at home.",
            CarePlan = "Daily walks and regular vet visits.",
            ReasonForApplying = "We have room for a family companion.",
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public void Application_ValidRequest_HasNoErrors() => Assert.Empty(Validate(Application()));

    [Fact]
    public void Application_NonPositivePetId_IsRejected()
    {
        Assert.True(Invalid(Application(a => a.PetId = 0), nameof(CreateApplicationRequest.PetId)));
    }

    [Fact]
    public void Application_MissingType_IsRejected()
    {
        Assert.True(Invalid(Application(a => a.Type = null), nameof(CreateApplicationRequest.Type)));
    }

    [Fact]
    public void Application_TooShortCarePlan_IsRejected()
    {
        Assert.True(Invalid(Application(a => a.CarePlan = "Walks."), nameof(CreateApplicationRequest.CarePlan)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(52, false)]
    [InlineData(53, true)]
    public void Application_FosterDurationIsOneToFiftyTwoWeeks(int weeks, bool invalid)
    {
        Assert.Equal(
            invalid,
            Invalid(Application(a => a.FosterDurationWeeks = weeks), nameof(CreateApplicationRequest.FosterDurationWeeks)));
    }

    [Fact]
    public void StatusChange_RequiresTheVersionTheClientSaw()
    {
        var withoutVersion = new UpdateApplicationStatusRequest { Status = ApplicationStatus.Approved };
        var valid = new UpdateApplicationStatusRequest { Status = ApplicationStatus.Approved, Version = 0 };

        Assert.True(Invalid(withoutVersion, nameof(UpdateApplicationStatusRequest.Version)));
        Assert.Empty(Validate(valid));
    }

    [Fact]
    public void StatusChange_NoteIsLimitedTo500Characters()
    {
        var request = new UpdateApplicationStatusRequest
        {
            Status = ApplicationStatus.Rejected,
            Version = 1,
            Note = new string('x', 501),
        };

        Assert.True(Invalid(request, nameof(UpdateApplicationStatusRequest.Note)));
    }

    /* ---------- moderation ---------- */

    [Fact]
    public void Report_RequiresAReason() =>
        Assert.True(Invalid(new CreateReportRequest(), nameof(CreateReportRequest.Reason)));

    [Fact]
    public void Report_DetailsAreLimitedTo1000Characters()
    {
        var request = new CreateReportRequest { Reason = ReportReason.Spam, Details = new string('x', 1001) };

        Assert.True(Invalid(request, nameof(CreateReportRequest.Details)));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("Checked and fine.", false)]
    public void ReviewReport_NoteMustBeMeaningful(string note, bool invalid)
    {
        var request = new ReviewReportRequest { Decision = ReportStatus.Resolved, Note = note };

        Assert.Equal(invalid, Invalid(request, nameof(ReviewReportRequest.Note)));
    }
    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    public void ChangePassword_ShortNewPassword_IsRejected(string password)
    {
        var request = new ChangePasswordRequest { CurrentPassword = "Old#Password2026", NewPassword = password };

        Assert.True(Invalid(request, nameof(ChangePasswordRequest.NewPassword)));
    }

    [Fact]
    public void ChangePassword_MissingCurrentPassword_IsRejected()
    {
        var request = new ChangePasswordRequest { NewPassword = "Brand#New2026Pass" };

        Assert.True(Invalid(request, nameof(ChangePasswordRequest.CurrentPassword)));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("A", true)]
    [InlineData("Sam", false)]
    public void UpdateProfile_DisplayNameNeedsAtLeastTwoCharacters(string name, bool invalid)
    {
        Assert.Equal(invalid, Invalid(new UpdateProfileRequest { DisplayName = name }, nameof(UpdateProfileRequest.DisplayName)));
    }


}