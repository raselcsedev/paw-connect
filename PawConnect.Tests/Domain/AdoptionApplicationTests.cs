using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Tests;

public class AdoptionApplicationTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static AdoptionApplication Create(
        ApplicationType type = ApplicationType.Adoption,
        ApplicationStatus status = ApplicationStatus.Pending)
    {
        var application = new AdoptionApplication { Type = type, Status = status };
        application.RefreshSlots();
        return application;
    }

    private static int? Slot(bool on) => on ? 1 : null;

    [Theory]
    [InlineData(ApplicationType.Adoption, ApplicationStatus.Pending, true, false)]
    [InlineData(ApplicationType.Adoption, ApplicationStatus.UnderReview, true, false)]
    [InlineData(ApplicationType.Adoption, ApplicationStatus.Approved, true, true)]
    [InlineData(ApplicationType.Foster, ApplicationStatus.Approved, true, true)]
    [InlineData(ApplicationType.Adoption, ApplicationStatus.Rejected, false, false)]
    [InlineData(ApplicationType.Foster, ApplicationStatus.Withdrawn, false, false)]
    public void RefreshSlots_FollowsTheStatus(
        ApplicationType type, ApplicationStatus status, bool active, bool approved)
    {
        var application = Create(type, status);

        Assert.Equal(Slot(active), application.ActiveSlot);
        Assert.Equal(Slot(approved), application.ApprovedSlot);
    }

    [Fact]
    public void ChangeStatus_ValidTransition_UpdatesStatusVersionSlotsAndHistory()
    {
        var application = Create();

        application.ChangeStatus(ApplicationStatus.Approved, ApplicationActor.PetOwner, OwnerId, "  Welcome home  ");

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(1, application.Version);
        Assert.Equal(1, application.ActiveSlot);
        Assert.Equal(1, application.ApprovedSlot);

        var entry = Assert.Single(application.History);
        Assert.Equal(ApplicationStatus.Pending, entry.FromStatus);
        Assert.Equal(ApplicationStatus.Approved, entry.ToStatus);
        Assert.Equal(OwnerId, entry.ChangedByUserId);
        Assert.Equal("Welcome home", entry.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeStatus_BlankNote_IsStoredAsNull(string? note)
    {
        var application = Create();

        application.ChangeStatus(ApplicationStatus.UnderReview, ApplicationActor.PetOwner, OwnerId, note);

        Assert.Null(Assert.Single(application.History).Note);
    }

    [Fact]
    public void ChangeStatus_RevokingAnApproval_FreesBothSlots()
    {
        var application = Create(status: ApplicationStatus.Approved);

        application.ChangeStatus(ApplicationStatus.Rejected, ApplicationActor.PetOwner, OwnerId, "Changed our minds.");

        Assert.Null(application.ActiveSlot);
        Assert.Null(application.ApprovedSlot);
    }

    [Fact]
    public void ChangeStatus_Withdrawn_ClearsTheActiveSlotSoTheApplicantCanApplyAgain()
    {
        var application = Create();

        application.ChangeStatus(ApplicationStatus.Withdrawn, ApplicationActor.Applicant, Guid.NewGuid(), null);

        Assert.Null(application.ActiveSlot);
    }

    [Fact]
    public void ChangeStatus_IllegalTransition_ThrowsAndLeavesTheApplicationUntouched()
    {
        var application = Create(status: ApplicationStatus.Rejected);

        Assert.Throws<ConflictException>(() =>
            application.ChangeStatus(ApplicationStatus.Approved, ApplicationActor.PetOwner, OwnerId, null));

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Equal(0, application.Version);
        Assert.Empty(application.History);
        Assert.Null(application.ActiveSlot);
    }

    [Fact]
    public void ChangeStatus_ApplicantCannotApproveTheirOwnApplication()
    {
        var application = Create();

        Assert.Throws<ConflictException>(() =>
            application.ChangeStatus(ApplicationStatus.Approved, ApplicationActor.Applicant, Guid.NewGuid(), null));
    }

    [Fact]
    public void ChangeStatus_ApplicantCannotWithdrawOnceReviewHasStarted()
    {
        var application = Create(status: ApplicationStatus.UnderReview);

        Assert.Throws<ConflictException>(() =>
            application.ChangeStatus(ApplicationStatus.Withdrawn, ApplicationActor.Applicant, Guid.NewGuid(), null));
    }

    [Fact]
    public void CompleteAdoption_ApprovedAdoption_RecordsTheEventButKeepsTheStatus()
    {
        var application = Create(status: ApplicationStatus.Approved);

        application.CompleteAdoption(OwnerId);

        Assert.NotNull(application.CompletedAtUtc);
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(1, application.Version);

        var entry = Assert.Single(application.History);
        Assert.Equal(ApplicationStatus.Approved, entry.FromStatus);
        Assert.Equal(ApplicationStatus.Approved, entry.ToStatus);
        Assert.Equal("Adoption completed.", entry.Note);
    }

    [Fact]
    public void CompleteAdoption_FosterApplication_IsRefused()
    {
        var application = Create(ApplicationType.Foster, ApplicationStatus.Approved);

        Assert.Throws<ConflictException>(() => application.CompleteAdoption(OwnerId));
    }

    [Theory]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void CompleteAdoption_WhenNotApproved_IsRefused(ApplicationStatus status)
    {
        var application = Create(status: status);

        Assert.Throws<ConflictException>(() => application.CompleteAdoption(OwnerId));
        Assert.Null(application.CompletedAtUtc);
    }

    [Fact]
    public void CompleteAdoption_Twice_IsRefused()
    {
        var application = Create(status: ApplicationStatus.Approved);
        application.CompleteAdoption(OwnerId);

        Assert.Throws<ConflictException>(() => application.CompleteAdoption(OwnerId));
    }

    [Fact]
    public void ChangeStatus_AfterCompletion_IsRefused()
    {
        var application = Create(status: ApplicationStatus.Approved);
        application.CompleteAdoption(OwnerId);

        Assert.Throws<ConflictException>(() =>
            application.ChangeStatus(ApplicationStatus.Rejected, ApplicationActor.PetOwner, OwnerId, "Too late."));
    }
}