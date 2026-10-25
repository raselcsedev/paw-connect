using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Tests;

public class ApplicationStatusRulesTests
{
    // The documented state machine. Everything not listed here must be refused.
    private static readonly HashSet<(ApplicationStatus, ApplicationStatus, ApplicationActor)> Allowed = new()
    {
        (ApplicationStatus.Pending, ApplicationStatus.Withdrawn, ApplicationActor.Applicant),
        (ApplicationStatus.Pending, ApplicationStatus.UnderReview, ApplicationActor.PetOwner),
        (ApplicationStatus.Pending, ApplicationStatus.Approved, ApplicationActor.PetOwner),
        (ApplicationStatus.Pending, ApplicationStatus.Rejected, ApplicationActor.PetOwner),
        (ApplicationStatus.UnderReview, ApplicationStatus.Approved, ApplicationActor.PetOwner),
        (ApplicationStatus.UnderReview, ApplicationStatus.Rejected, ApplicationActor.PetOwner),
        (ApplicationStatus.Approved, ApplicationStatus.Rejected, ApplicationActor.PetOwner),
    };

    public static TheoryData<ApplicationStatus, ApplicationStatus, ApplicationActor> AllCombinations()
    {
        var data = new TheoryData<ApplicationStatus, ApplicationStatus, ApplicationActor>();

        foreach (var from in Enum.GetValues<ApplicationStatus>())
        foreach (var to in Enum.GetValues<ApplicationStatus>())
        foreach (var actor in Enum.GetValues<ApplicationActor>())
        {
            data.Add(from, to, actor);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public void CanTransition_MatchesTheDocumentedStateMachine(
        ApplicationStatus from, ApplicationStatus to, ApplicationActor actor)
    {
        var expected = Allowed.Contains((from, to, actor));

        Assert.Equal(expected, ApplicationStatusRules.CanTransition(from, to, actor));
    }

    [Fact]
    public void TerminalStatuses_HaveNoWayOut()
    {
        foreach (var from in new[] { ApplicationStatus.Rejected, ApplicationStatus.Withdrawn })
        foreach (var to in Enum.GetValues<ApplicationStatus>())
        foreach (var actor in Enum.GetValues<ApplicationActor>())
        {
            Assert.False(ApplicationStatusRules.CanTransition(from, to, actor), $"{from} -> {to} by {actor}");
        }
    }

    [Theory]
    [InlineData(ApplicationStatus.Pending, true)]
    [InlineData(ApplicationStatus.UnderReview, true)]
    [InlineData(ApplicationStatus.Approved, true)]
    [InlineData(ApplicationStatus.Rejected, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void IsActive_OnlyOpenStatusesCount(ApplicationStatus status, bool expected)
    {
        Assert.Equal(expected, ApplicationStatusRules.IsActive(status));
    }
}