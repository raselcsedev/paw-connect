using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Dashboard;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class DashboardService : IDashboardService
{
    private const int RecentCount = 5;

    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApplicantDashboardDto> GetApplicantAsync(Guid userId, CancellationToken cancellationToken)
    {
        var statusRows = await _db.Applications
            .AsNoTracking()
            .Where(a => a.ApplicantId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = ToCounts<ApplicationStatus>(statusRows.Select(r => (r.Key, r.Count)));

        var recent = await _db.Applications
            .AsNoTracking()
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Take(RecentCount)
            .Select(a => new ApplicationSummaryDto(
                a.Id, a.PetId, a.Pet.Name, a.Type, a.Status,
                a.FosterStartDate, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var unread = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        var active = byStatus[nameof(ApplicationStatus.Pending)]
                     + byStatus[nameof(ApplicationStatus.UnderReview)]
                     + byStatus[nameof(ApplicationStatus.Approved)];

        return new ApplicantDashboardDto(byStatus.Values.Sum(), active, byStatus, unread, recent);
    }

    public async Task<RescuerDashboardDto> GetRescuerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var petRows = await _db.Pets
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var petsByStatus = ToCounts<ListingStatus>(petRows.Select(r => (r.Key, r.Count)));

        var applicationRows = await _db.Applications
            .AsNoTracking()
            .Where(a => a.Pet.OwnerId == ownerId)
            .GroupBy(a => a.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var applicationsByStatus = ToCounts<ApplicationStatus>(applicationRows.Select(r => (r.Key, r.Count)));

        var awaitingCompletion = await _db.Applications.CountAsync(
            a => a.Pet.OwnerId == ownerId
                 && a.Type == ApplicationType.Adoption
                 && a.Status == ApplicationStatus.Approved
                 && a.CompletedAtUtc == null,
            cancellationToken);

        var completed = await _db.Applications.CountAsync(
            a => a.Pet.OwnerId == ownerId && a.CompletedAtUtc != null,
            cancellationToken);

        var recentOpen = await _db.Applications
            .AsNoTracking()
            .Where(a => a.Pet.OwnerId == ownerId
                        && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.UnderReview))
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Take(RecentCount)
            .Select(a => new IncomingApplicationDto(
                a.Id, a.PetId, a.Pet.Name, a.Applicant.DisplayName, a.Type, a.Status,
                a.FosterStartDate, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var unread = await _db.Notifications
            .CountAsync(n => n.UserId == ownerId && !n.IsRead, cancellationToken);

        var awaitingReview = applicationsByStatus[nameof(ApplicationStatus.Pending)]
                             + applicationsByStatus[nameof(ApplicationStatus.UnderReview)];

        return new RescuerDashboardDto(
            petsByStatus.Values.Sum(),
            petsByStatus,
            applicationsByStatus.Values.Sum(),
            applicationsByStatus,
            awaitingReview,
            awaitingCompletion,
            completed,
            unread,
            recentOpen);
    }

    public async Task<AdminDashboardDto> GetAdminAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var since7 = now.AddDays(-7);
        var since30 = now.AddDays(-30);

        var userStatusRows = await _db.Users
            .AsNoTracking()
            .GroupBy(u => u.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var usersByStatus = ToCounts<AccountStatus>(userStatusRows.Select(r => (r.Key, r.Count)));

        var roleRows = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            group r by r.Name into g
            select new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var usersByRole = Roles.All.ToDictionary(role => role, _ => 0);
        foreach (var row in roleRows)
        {
            if (row.Role is not null)
            {
                usersByRole[row.Role] = row.Count;
            }
        }

        var new7 = await _db.Users.CountAsync(u => u.CreatedAtUtc >= since7, cancellationToken);
        var new30 = await _db.Users.CountAsync(u => u.CreatedAtUtc >= since30, cancellationToken);

        var petRows = await _db.Pets
            .AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var petsByStatus = ToCounts<ListingStatus>(petRows.Select(r => (r.Key, r.Count)));

        var applicationStatusRows = await _db.Applications
            .AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var applicationsByStatus = ToCounts<ApplicationStatus>(applicationStatusRows.Select(r => (r.Key, r.Count)));

        var applicationTypeRows = await _db.Applications
            .AsNoTracking()
            .GroupBy(a => a.Type)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var applicationsByType = ToCounts<ApplicationType>(applicationTypeRows.Select(r => (r.Key, r.Count)));

        var completed = await _db.Applications.CountAsync(a => a.CompletedAtUtc != null, cancellationToken);

        return new AdminDashboardDto(
            now,
            new UserStatsDto(usersByStatus.Values.Sum(), usersByStatus, usersByRole, new7, new30),
            new PetStatsDto(petsByStatus.Values.Sum(), petsByStatus),
            new ApplicationStatsDto(
                applicationsByStatus.Values.Sum(), applicationsByStatus, applicationsByType, completed));
    }

    private static Dictionary<string, int> ToCounts<TEnum>(IEnumerable<(TEnum Key, int Count)> rows)
        where TEnum : struct, Enum
    {
        var result = Enum.GetValues<TEnum>().ToDictionary(v => v.ToString(), _ => 0);

        foreach (var (key, count) in rows)
        {
            result[key.ToString()] = count;
        }

        return result;
    }
}