using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;
using static PawConnect.Tests.Integration.Workflow;


namespace PawConnect.Tests.Integration;

/// <summary>
/// These tests go around the API and write to the database directly,
/// to prove the database itself enforces the rules, even if some future code forgets to.
/// </summary>
public class DatabaseConstraintTests(ApiFactory factory)
{
    private const int ParentRowIsReferenced = 1451;

    private static async Task<MySqlException> Violation(Func<Task> action)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(action);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysql)
            {
                return mysql;
            }
        }

        throw new InvalidOperationException("Expected a MySQL error, but got: " + exception);
    }

    private static AdoptionApplication Row(
        int petId, Guid applicantId, ApplicationStatus status = ApplicationStatus.Pending,
        ApplicationType type = ApplicationType.Adoption)
    {
        var application = new AdoptionApplication
        {
            PetId = petId,
            ApplicantId = applicantId,
            Type = type,
            LivingArrangement = LivingArrangement.House,
            HouseholdInfo = "x",
            CarePlan = "x",
            ReasonForApplying = "x",
            Status = status,
        };

        application.RefreshSlots();
        return application;
    }

    private async Task<(TestUser Rescuer, PetDto Pet)> PublishedPet()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        return (rescuer, await factory.CreateAvailablePetAsync(rescuer, AvailabilityType.Adoption));
    }

    /* ---------- unique constraints ---------- */

    [Fact]
    public async Task ThereCanOnlyBeOneActiveApplication_PerPetAndApplicant()
    {
        var (_, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        await factory.SubmitAsync(adopter, pet.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();
        db.Applications.Add(Row(pet.Id, adopter.Id));

        var error = await Violation(() => db.SaveChangesAsync(Ct));

        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, error.ErrorCode);
    }

    [Fact]
    public async Task FinishedApplications_NeverBlockANewOneOrEachOther()
    {
        var (_, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        var first = await factory.SubmitAsync(adopter, pet.Id);
        await (await factory.WithdrawRawAsync(adopter, first.Id)).ShouldBe(System.Net.HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        db.Applications.Add(Row(pet.Id, adopter.Id));                                      // a fresh active one
        db.Applications.Add(Row(pet.Id, adopter.Id, ApplicationStatus.Rejected));          // finished rows have a NULL slot,
        db.Applications.Add(Row(pet.Id, adopter.Id, ApplicationStatus.Withdrawn));         // and NULLs never collide
        await db.SaveChangesAsync(Ct);

        Assert.Equal(4, await db.Applications.CountAsync(a => a.PetId == pet.Id && a.ApplicantId == adopter.Id, Ct));
    }

    [Fact]
    public async Task ThereCanOnlyBeOneApprovedApplication_PerPet()
    {
        var (rescuer, pet) = await PublishedPet();
        var first = await factory.RegisterAsync();
        var second = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(first, pet.Id);
        await factory.ChangeStatusAsync(rescuer, application.Id, ApplicationStatus.Approved, application.Version);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();
        db.Applications.Add(Row(pet.Id, second.Id, ApplicationStatus.Approved));

        var error = await Violation(() => db.SaveChangesAsync(Ct));

        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, error.ErrorCode);
    }

    [Fact]
    public async Task ThereCanOnlyBeOneOpenReport_PerPetAndReporter_ButClosedOnesDoNotCount()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var reporter = await factory.RegisterAsync();
        var pet = await factory.CreateAvailablePetAsync(owner);

        var response = await factory.As(reporter).SendJsonAsync(
            HttpMethod.Post, $"/api/pets/{pet.Id}/reports", new { reason = "Spam" });
        await response.ShouldBe(System.Net.HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        db.Reports.Add(new Report { PetId = pet.Id, ReporterId = reporter.Id, Reason = ReportReason.Spam });
        var error = await Violation(() => db.SaveChangesAsync(Ct));
        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, error.ErrorCode);

        db.ChangeTracker.Clear();
        db.Reports.Add(new Report
        {
            PetId = pet.Id,
            ReporterId = reporter.Id,
            Reason = ReportReason.Spam,
            Status = ReportStatus.Dismissed,
            OpenSlot = null,
        });
        await db.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task StoredPhotoFileNames_AreUnique()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateDraftPetAsync(owner);
        var name = $"{Guid.NewGuid():N}.png";

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();
        db.PetPhotos.Add(new PetPhoto { PetId = pet.Id, StoredFileName = name, ContentType = "image/png", SizeInBytes = 1 });
        await db.SaveChangesAsync(Ct);

        db.PetPhotos.Add(new PetPhoto { PetId = pet.Id, StoredFileName = name, ContentType = "image/png", SizeInBytes = 1 });
        var error = await Violation(() => db.SaveChangesAsync(Ct));

        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, error.ErrorCode);
    }

    /* ---------- foreign keys ---------- */

    [Fact]
    public async Task APetWithApplications_CannotBeDeleted_SoHistoryIsNeverLost()
    {
        var (_, pet) = await PublishedPet();
        await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        var error = await Violation(() => db.Pets.Where(p => p.Id == pet.Id).ExecuteDeleteAsync(Ct));

        Assert.Equal(ParentRowIsReferenced, (int)error.ErrorCode);
    }

    [Fact]
    public async Task AUserWhoOwnsPetsOrHasApplications_CannotBeDeleted()
    {
        var (rescuer, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        await factory.SubmitAsync(adopter, pet.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        var owner = await Violation(() => db.Users.Where(u => u.Id == rescuer.Id).ExecuteDeleteAsync(Ct));
        var applicant = await Violation(() => db.Users.Where(u => u.Id == adopter.Id).ExecuteDeleteAsync(Ct));

        Assert.Equal(ParentRowIsReferenced, (int)owner.ErrorCode);
        Assert.Equal(ParentRowIsReferenced, (int)applicant.ErrorCode);
    }

    [Fact]
    public async Task DeletingAPetWithoutHistory_RemovesItsPhotoRows()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateDraftPetAsync(owner);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();
        db.PetPhotos.AddRange(
            new PetPhoto { PetId = pet.Id, StoredFileName = $"{Guid.NewGuid():N}.png", ContentType = "image/png", SizeInBytes = 1 },
            new PetPhoto { PetId = pet.Id, StoredFileName = $"{Guid.NewGuid():N}.png", ContentType = "image/png", SizeInBytes = 1 });
        await db.SaveChangesAsync(Ct);

        await db.Pets.Where(p => p.Id == pet.Id).ExecuteDeleteAsync(Ct);

        Assert.Equal(0, await db.PetPhotos.CountAsync(p => p.PetId == pet.Id, Ct));
    }

    /* ---------- optimistic concurrency ---------- */

    [Fact]
    public async Task TwoStaleCopiesOfAnApplication_CannotBothBeSaved()
    {
        var (rescuer, pet) = await PublishedPet();
        var application = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var dbA = scopeA.Db();
        var dbB = scopeB.Db();

        var a = await dbA.Applications.SingleAsync(x => x.Id == application.Id, Ct);
        var b = await dbB.Applications.SingleAsync(x => x.Id == application.Id, Ct);

        a.ChangeStatus(ApplicationStatus.UnderReview, ApplicationActor.PetOwner, rescuer.Id, null);
        await dbA.SaveChangesAsync(Ct);

        b.ChangeStatus(ApplicationStatus.Approved, ApplicationActor.PetOwner, rescuer.Id, null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task TwoStaleCopiesOfAReport_CannotBothBeReviewed()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var reporter = await factory.RegisterAsync();
        var pet = await factory.CreateAvailablePetAsync(owner);

        var response = await factory.As(reporter).SendJsonAsync(
            HttpMethod.Post, $"/api/pets/{pet.Id}/reports", new { reason = "Spam" });
        var reportId = (await response.ReadAsync<PawConnect.Api.Application.DTOs.Moderation.ReportDto>()).Id;

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var dbA = scopeA.Db();
        var dbB = scopeB.Db();

        var a = await dbA.Reports.SingleAsync(r => r.Id == reportId, Ct);
        var b = await dbB.Reports.SingleAsync(r => r.Id == reportId, Ct);

        a.Review(ReportStatus.Dismissed, admin.Id, "First decision.");
        await dbA.SaveChangesAsync(Ct);

        b.Review(ReportStatus.Resolved, admin.Id, "Second decision.");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync(Ct));
    }

    /* ---------- how data is stored ---------- */

    [Fact]
    public async Task Enums_AreStoredAsReadableText_NotAsNumbers()
    {
        var (_, pet) = await PublishedPet();
        var application = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        var applicationStatus = await db.Database
            .SqlQueryRaw<string>("SELECT Status AS Value FROM Applications WHERE Id = {0}", application.Id)
            .SingleAsync(Ct);
        var petStatus = await db.Database
            .SqlQueryRaw<string>("SELECT Status AS Value FROM Pets WHERE Id = {0}", pet.Id)
            .SingleAsync(Ct);
        var species = await db.Database
            .SqlQueryRaw<string>("SELECT Species AS Value FROM Pets WHERE Id = {0}", pet.Id)
            .SingleAsync(Ct);

        Assert.Equal("Pending", applicationStatus);
        Assert.Equal("Available", petStatus);
        Assert.Equal("Dog", species);
    }

    [Fact]
    public async Task PasswordsAreStoredOnlyAsHashes_AndRefreshTokensOnlyAsHashes()
    {
        var user = await factory.RegisterAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.Db();

        var hash = await db.Users.Where(u => u.Id == user.Id).Select(u => u.PasswordHash).SingleAsync(Ct);
        Assert.NotNull(hash);
        Assert.DoesNotContain(user.Password, hash);

        var storedTokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).Select(t => t.TokenHash).ToListAsync(Ct);
        Assert.NotEmpty(storedTokens);
        Assert.DoesNotContain(user.RefreshToken, storedTokens);
        Assert.All(storedTokens, stored => Assert.Equal(64, stored.Length));
    }
}