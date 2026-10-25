using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Tests;

public class ReportTests
{
    [Fact]
    public void NewReport_IsOpenAndOccupiesTheOpenSlot()
    {
        var report = new Report();

        Assert.Equal(ReportStatus.Open, report.Status);
        Assert.Equal(1, report.OpenSlot);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void Review_ClosesTheReportAndFreesTheOpenSlot(ReportStatus decision)
    {
        var report = new Report();
        var reviewer = Guid.NewGuid();

        report.Review(decision, reviewer, "Checked the listing.");

        Assert.Equal(decision, report.Status);
        Assert.Null(report.OpenSlot);
        Assert.Equal(reviewer, report.ReviewedByUserId);
        Assert.NotNull(report.ReviewedAtUtc);
        Assert.Equal("Checked the listing.", report.ResolutionNote);
    }

    [Fact]
    public void Review_Twice_IsRefused_SoTwoAdminsCannotBothDecide()
    {
        var report = new Report();
        report.Review(ReportStatus.Resolved, Guid.NewGuid(), "First.");

        Assert.Throws<ConflictException>(() => report.Review(ReportStatus.Dismissed, Guid.NewGuid(), "Second."));
        Assert.Equal(ReportStatus.Resolved, report.Status);
    }

    [Fact]
    public void Review_WithAnOpenDecision_IsAProgrammingError()
    {
        Assert.Throws<ArgumentException>(() => new Report().Review(ReportStatus.Open, Guid.NewGuid(), "Nope."));
    }
}