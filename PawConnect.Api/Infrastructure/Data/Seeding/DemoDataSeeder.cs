using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Infrastructure.Data.Seeding;

/// <summary>
/// Development only. Fills the local database with demo accounts, pets, photos, applications and reports.
/// It only creates what is missing, so it is safe to run on every start.
/// </summary>
public static class DemoDataSeeder
{
    public const string DefaultPassword = "Demo#Paws2026";

    private const string AdminEmail = "demo.admin@example.com";
    private const string Rescuer1Email = "demo.rescuer@example.com";
    private const string Rescuer2Email = "second.rescuer@example.com";
    private const string SamEmail = "sam.adopter@example.com";
    private const string NadiaEmail = "nadia.adopter@example.com";
    private const string OmarEmail = "omar.adopter@example.com";

    private sealed record DemoUser(string Email, string Name, string Role);

    private sealed record DemoPet(
        string Name, Species Species, string Breed, int AgeMonths, PetSize Size, PetSex Sex,
        string Description, bool Vaccinated, bool Neutered, AvailabilityType Availability,
        string City, string Area, ListingStatus Status, int Owner,
        string? Health = null, string? Care = null, string? Requirements = null, string? ModerationNote = null);

    private enum Outcome { Pending, UnderReview, Approved, Completed, Rejected, AutoRejected, Withdrawn }

    private sealed record DemoApplication(string Applicant, string Pet, ApplicationType Type, Outcome Outcome, int DaysAgo);

    private static readonly DemoUser[] DemoUsers =
    [
        new(AdminEmail, "Demo Administrator", Roles.Admin),
        new(Rescuer1Email, "Demo Rescue Shelter", Roles.Rescuer),
        new(Rescuer2Email, "Street Paws Dhaka", Roles.Rescuer),
        new(SamEmail, "Sam Adopter", Roles.Adopter),
        new(NadiaEmail, "Nadia Rahman", Roles.Adopter),
        new(OmarEmail, "Omar Hasan", Roles.Adopter),
    ];

    // Owner 0 = Demo Rescue Shelter, owner 1 = Street Paws Dhaka.
    private static readonly DemoPet[] DemoPets =
    [
        new("Buddy", Species.Dog, "Labrador mix", 18, PetSize.Medium, PetSex.Male,
            "Buddy is a gentle, food-motivated dog who loves fetch and long walks. He is good with children and other dogs.",
            true, true, AvailabilityType.Both, "Dhaka", "Mirpur", ListingStatus.Available, 0,
            Health: "Healthy, dewormed and microchipped.", Requirements: "A home with a garden, or daily access to a park."),
        new("Luna", Species.Cat, "Domestic shorthair", 10, PetSize.Small, PetSex.Female,
            "Luna is a curious young cat who starts purring the moment you pick her up. She needs a temporary home while her rescuer travels.",
            true, false, AvailabilityType.Foster, "Dhaka", "Dhanmondi", ListingStatus.Available, 0,
            Requirements: "Indoor home only."),
        new("Rocky", Species.Dog, "Beagle", 36, PetSize.Medium, PetSex.Male,
            "Rocky found his forever family last month. Thank you to everyone who applied.",
            true, true, AvailabilityType.Adoption, "Dhaka", "Gulshan", ListingStatus.Adopted, 0),
        new("Cleo", Species.Cat, "Persian mix", 48, PetSize.Small, PetSex.Female,
            "Cleo is a calm lap cat who enjoys quiet evenings and sunny windowsills.",
            true, true, AvailabilityType.Adoption, "Chattogram", "Panchlaish", ListingStatus.Available, 0,
            Care: "Daily brushing; sensitive eyes need wiping."),
        new("Mango", Species.Rabbit, "Holland Lop", 14, PetSize.Small, PetSex.Female,
            "Mango is a friendly rabbit who loves fresh vegetables and gentle strokes.",
            true, false, AvailabilityType.Both, "Dhaka", "Uttara", ListingStatus.Available, 0),
        new("Coco", Species.Bird, "Cockatiel", 24, PetSize.Small, PetSex.Unknown,
            "Coco whistles little tunes and loves company. Best with someone who is home a lot.",
            false, false, AvailabilityType.Adoption, "Dhaka", "Banani", ListingStatus.Available, 0,
            Care: "Needs a large cage and daily out-of-cage time."),
        new("Max", Species.Dog, "German Shepherd", 60, PetSize.Large, PetSex.Male,
            "Max is loyal and well trained. He thrives with an active, experienced owner.",
            true, true, AvailabilityType.Adoption, "Chattogram", "Agrabad", ListingStatus.Available, 0,
            Requirements: "Experienced dog owner. No small children."),
        new("Biscuit", Species.Dog, "Indian Pariah", 8, PetSize.Medium, PetSex.Female,
            "Biscuit is a playful puppy who is still learning her manners and loves everyone she meets.",
            true, false, AvailabilityType.Both, "Dhaka", "Mohammadpur", ListingStatus.Available, 0),
        new("Pixel", Species.Cat, "Tabby", 6, PetSize.Small, PetSex.Male,
            "Draft listing: photos and details still being prepared.",
            false, false, AvailabilityType.Adoption, "Dhaka", "Mirpur", ListingStatus.Draft, 0),
        new("Shadow", Species.Dog, "Mixed breed", 30, PetSize.Large, PetSex.Male,
            "Shadow is a quiet, watchful dog.",
            true, true, AvailabilityType.Adoption, "Dhaka", "Gulshan", ListingStatus.Unavailable, 0,
            ModerationNote: "Photos did not match the animal. Please upload new photos."),
        new("Maple", Species.Rabbit, "Rex", 20, PetSize.Small, PetSex.Male,
            "Maple is paused while his rescuer arranges a vet check.",
            true, true, AvailabilityType.Both, "Dhaka", "Dhanmondi", ListingStatus.Unavailable, 0),
        new("Tiger", Species.Cat, "Bengal mix", 22, PetSize.Medium, PetSex.Male,
            "Tiger is an energetic climber who needs plenty of play and a secure balcony or garden.",
            true, true, AvailabilityType.Adoption, "Dhaka", "Dhanmondi", ListingStatus.Available, 1),
        new("Daisy", Species.Dog, "Spitz mix", 12, PetSize.Small, PetSex.Female,
            "Daisy is a fluffy, cheerful dog who gets along with everyone.",
            true, false, AvailabilityType.Both, "Sylhet", "Zindabazar", ListingStatus.Available, 1),
        new("Oscar", Species.Dog, "Rottweiler mix", 72, PetSize.Large, PetSex.Male,
            "Oscar is a gentle giant who loves slow walks and long naps.",
            true, true, AvailabilityType.Adoption, "Khulna", "Sonadanga", ListingStatus.Available, 1,
            Health: "Mild arthritis; supplements are provided."),
        new("Nala", Species.Cat, "Siamese mix", 16, PetSize.Small, PetSex.Female,
            "Nala is talkative and affectionate, and she needs a foster home for a few weeks.",
            true, true, AvailabilityType.Foster, "Dhaka", "Uttara", ListingStatus.Available, 1),
        new("Teddy", Species.Dog, "Golden retriever mix", 4, PetSize.Medium, PetSex.Male,
            "Teddy is a bouncy puppy who is ready for training classes and lots of cuddles.",
            true, false, AvailabilityType.Both, "Dhaka", "Gulshan", ListingStatus.Available, 1),
        new("Olive", Species.Rabbit, "Dutch", 30, PetSize.Small, PetSex.Female,
            "Olive is calm and tidy, and loves a quiet corner with fresh hay.",
            true, true, AvailabilityType.Adoption, "Sylhet", "Ambarkhana", ListingStatus.Available, 1),
        new("Kiwi", Species.Bird, "Budgerigar", 10, PetSize.Small, PetSex.Unknown,
            "Kiwi is a cheerful budgie who loves mirrors and millet.",
            false, false, AvailabilityType.Adoption, "Dhaka", "Mirpur", ListingStatus.Available, 1),
        new("Ginger", Species.Cat, "Orange tabby", 40, PetSize.Medium, PetSex.Male,
            "Ginger is a relaxed, easy-going cat who is happy anywhere there is a warm lap.",
            true, true, AvailabilityType.Both, "Rajshahi", "Boalia", ListingStatus.Available, 1),
        new("Lily", Species.Dog, "Labrador", 96, PetSize.Large, PetSex.Female,
            "Lily is a sweet senior dog who deserves a comfortable retirement with a loving family.",
            true, true, AvailabilityType.Adoption, "Dhaka", "Banani", ListingStatus.Available, 1,
            Care: "Joint supplements and gentle exercise."),
        new("Ziggy", Species.Other, "Syrian hamster", 5, PetSize.Small, PetSex.Male,
            "Ziggy is an active little hamster who loves his wheel and tunnels.",
            false, false, AvailabilityType.Adoption, "Dhaka", "Mirpur", ListingStatus.Available, 1),
        new("Ruby", Species.Dog, "Mixed breed", 26, PetSize.Medium, PetSex.Female,
            "This listing was archived by the rescuer.",
            true, true, AvailabilityType.Adoption, "Dhaka", "Gulshan", ListingStatus.Archived, 1),
    ];

    private static readonly DemoApplication[] DemoApplications =
    [
        new(SamEmail, "Buddy", ApplicationType.Adoption, Outcome.UnderReview, 6),
        new(NadiaEmail, "Buddy", ApplicationType.Adoption, Outcome.Pending, 3),
        new(OmarEmail, "Luna", ApplicationType.Foster, Outcome.Approved, 9),
        new(SamEmail, "Cleo", ApplicationType.Adoption, Outcome.Approved, 12),
        new(NadiaEmail, "Rocky", ApplicationType.Adoption, Outcome.Completed, 30),
        new(OmarEmail, "Rocky", ApplicationType.Adoption, Outcome.AutoRejected, 28),
        new(SamEmail, "Mango", ApplicationType.Adoption, Outcome.Rejected, 15),
        new(OmarEmail, "Biscuit", ApplicationType.Adoption, Outcome.Withdrawn, 7),
        new(SamEmail, "Tiger", ApplicationType.Adoption, Outcome.Pending, 2),
        new(NadiaEmail, "Teddy", ApplicationType.Foster, Outcome.Pending, 1),
        new(OmarEmail, "Nala", ApplicationType.Foster, Outcome.UnderReview, 4),
    ];

    private static readonly LivingArrangement[] Arrangements =
        [LivingArrangement.House, LivingArrangement.Apartment, LivingArrangement.SharedHousing];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();

        if (string.Equals(configuration["Seed:DemoData"], "false", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var storage = services.GetRequiredService<IFileStorage>();
        var password = configuration["Seed:DemoPassword"] ?? DefaultPassword;

        var accounts = await EnsureUsersAsync(users, password);
        var pets = await EnsurePetsAsync(db, accounts, cancellationToken);
        await EnsurePhotosAsync(db, storage, accounts, cancellationToken);
        await EnsureApplicationsAsync(db, accounts, pets, cancellationToken);
        await EnsureReportsAsync(db, accounts, pets, cancellationToken);

        logger.LogInformation(
            "Demo data is ready. Demo accounts: {Emails}",
            string.Join(", ", DemoUsers.Select(user => user.Email)));
    }

    /* ---------- accounts ---------- */

    private static async Task<Dictionary<string, ApplicationUser>> EnsureUsersAsync(
        UserManager<ApplicationUser> users, string password)
    {
        var accounts = new Dictionary<string, ApplicationUser>();

        foreach (var demo in DemoUsers)
        {
            var user = await users.FindByEmailAsync(demo.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = demo.Email,
                    Email = demo.Email,
                    EmailConfirmed = true,
                    DisplayName = demo.Name,
                    Status = AccountStatus.Active,
                };

                var created = await users.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create demo user {demo.Email}: " +
                        string.Join("; ", created.Errors.Select(error => error.Description)));
                }

                await users.AddToRoleAsync(user, demo.Role);
            }
            else
            {
                // Demo accounts are always usable, with the known demo password.
                user.Status = AccountStatus.Active;
                user.DisplayName = demo.Name;
                await users.UpdateAsync(user);
                await users.SetLockoutEndDateAsync(user, null);
                await users.ResetAccessFailedCountAsync(user);

                if (!await users.CheckPasswordAsync(user, password))
                {
                    await users.RemovePasswordAsync(user);
                    var added = await users.AddPasswordAsync(user, password);

                    if (!added.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Could not reset the password of {demo.Email}: " +
                            string.Join("; ", added.Errors.Select(error => error.Description)));
                    }
                }

                if (!await users.IsInRoleAsync(user, demo.Role))
                {
                    await users.AddToRoleAsync(user, demo.Role);
                }
            }

            accounts[demo.Email] = user;
        }

        return accounts;
    }

    private static ApplicationUser[] Owners(Dictionary<string, ApplicationUser> accounts) =>
        [accounts[Rescuer1Email], accounts[Rescuer2Email]];

    /* ---------- pets ---------- */

    private static async Task<Dictionary<string, Pet>> EnsurePetsAsync(
        AppDbContext db, Dictionary<string, ApplicationUser> accounts, CancellationToken cancellationToken)
    {
        var owners = Owners(accounts);
        var ownerIds = owners.Select(owner => owner.Id).ToList();

        var existing = await db.Pets
            .Where(pet => ownerIds.Contains(pet.OwnerId))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, Pet>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < DemoPets.Length; i++)
        {
            var demo = DemoPets[i];
            var owner = owners[demo.Owner];
            var pet = existing.FirstOrDefault(p => p.OwnerId == owner.Id && p.Name == demo.Name);

            if (pet is null)
            {
                var created = now.AddHours(-(DemoPets.Length - i) * 9);

                pet = new Pet
                {
                    OwnerId = owner.Id,
                    Name = demo.Name,
                    Species = demo.Species,
                    Breed = demo.Breed,
                    AgeInMonths = demo.AgeMonths,
                    Size = demo.Size,
                    Sex = demo.Sex,
                    Description = demo.Description,
                    HealthStatus = demo.Health,
                    IsVaccinated = demo.Vaccinated,
                    IsSpayedNeutered = demo.Neutered,
                    SpecialCareRequirements = demo.Care,
                    AvailabilityType = demo.Availability,
                    Requirements = demo.Requirements,
                    City = demo.City,
                    Area = demo.Area,
                    Status = demo.Status,
                    IsSuspended = demo.ModerationNote is not null,
                    ModerationNote = demo.ModerationNote,
                    CreatedAtUtc = created,
                    UpdatedAtUtc = created,
                };

                db.Pets.Add(pet);
            }

            result[demo.Name] = pet;
        }

        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    /* ---------- photos ---------- */

    private static async Task EnsurePhotosAsync(
        AppDbContext db, IFileStorage storage, Dictionary<string, ApplicationUser> accounts,
        CancellationToken cancellationToken)
    {
        var ownerIds = Owners(accounts).Select(owner => owner.Id).ToList();

        var pets = await db.Pets
            .Where(pet => ownerIds.Contains(pet.OwnerId))
            .Include(pet => pet.Photos)
            .ToListAsync(cancellationToken);

        foreach (var pet in pets)
        {
            var wanted = pet.Status == ListingStatus.Draft ? 1 : 2;

            while (pet.Photos.Count < wanted)
            {
                var bytes = PlaceholderImage.For(pet.Species, pet.Id + pet.Photos.Count);
                var name = $"{Guid.NewGuid():N}.png";

                await WriteAsync(storage, name, bytes, cancellationToken);

                pet.Photos.Add(new PetPhoto
                {
                    PetId = pet.Id,
                    StoredFileName = name,
                    ContentType = "image/png",
                    SizeInBytes = bytes.Length,
                    IsPrimary = pet.Photos.Count == 0,
                });
            }

            // If a picture file went missing (for example the storage folder was cleared), recreate it.
            foreach (var photo in pet.Photos)
            {
                await using var stream = await storage.OpenReadAsync(photo.StoredFileName, cancellationToken);

                if (stream is null)
                {
                    var bytes = PlaceholderImage.For(pet.Species, photo.Id);
                    await WriteAsync(storage, photo.StoredFileName, bytes, cancellationToken);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task WriteAsync(IFileStorage storage, string name, byte[] bytes, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(bytes);
        await storage.SaveAsync(name, stream, cancellationToken);
    }

    /* ---------- applications ---------- */

    private static bool Accepts(Pet pet, ApplicationType type) => type == ApplicationType.Adoption
        ? pet.AvailabilityType is AvailabilityType.Adoption or AvailabilityType.Both
        : pet.AvailabilityType is AvailabilityType.Foster or AvailabilityType.Both;

    private static DateTime Clamp(DateTime value, DateTime now) => value > now ? now : value;

    private static async Task EnsureApplicationsAsync(
        AppDbContext db, Dictionary<string, ApplicationUser> accounts, Dictionary<string, Pet> pets,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        for (var index = 0; index < DemoApplications.Length; index++)
        {
            var demo = DemoApplications[index];

            if (!accounts.TryGetValue(demo.Applicant, out var applicant) || !pets.TryGetValue(demo.Pet, out var pet))
            {
                continue;
            }

            if (!Accepts(pet, demo.Type))
            {
                continue;
            }

            var petId = pet.Id;
            var applicantId = applicant.Id;

            if (await db.Applications.AnyAsync(a => a.PetId == petId && a.ApplicantId == applicantId, cancellationToken))
            {
                continue;
            }

            var owner = accounts.Values.First(user => user.Id == pet.OwnerId);
            var foster = demo.Type == ApplicationType.Foster;
            var created = now.AddDays(-demo.DaysAgo);

            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                ApplicantId = applicant.Id,
                Type = demo.Type,
                LivingArrangement = Arrangements[index % Arrangements.Length],
                HouseholdInfo = "Two adults and one child, aged 9. We live in a quiet neighbourhood.",
                OtherPetsInfo = index % 2 == 0 ? "One friendly, neutered cat." : null,
                PreviousExperience = "We have looked after pets for many years and know how much time and care they need.",
                CarePlan = "Someone is home most of the day. Daily walks and play, regular vet visits, and a safe, comfortable bed.",
                ReasonForApplying = $"We would love to give {pet.Name} a loving, patient home.",
                FosterStartDate = foster ? today.AddDays(10) : null,
                FosterDurationWeeks = foster ? 6 : null,
                CreatedAtUtc = created,
                UpdatedAtUtc = created,
            };

            application.RefreshSlots();
            application.History.Add(new ApplicationStatusHistory
            {
                FromStatus = null,
                ToStatus = ApplicationStatus.Pending,
                ChangedByUserId = applicant.Id,
                ChangedAtUtc = created,
                Note = "Application submitted.",
            });

            switch (demo.Outcome)
            {
                case Outcome.UnderReview:
                    application.ChangeStatus(ApplicationStatus.UnderReview, ApplicationActor.PetOwner, owner.Id, null);
                    break;
                case Outcome.Approved:
                    application.ChangeStatus(
                        ApplicationStatus.Approved, ApplicationActor.PetOwner, owner.Id, "Great home visit. Welcome to the family!");
                    break;
                case Outcome.Completed:
                    application.ChangeStatus(
                        ApplicationStatus.Approved, ApplicationActor.PetOwner, owner.Id, "Approved after a successful home visit.");
                    application.CompleteAdoption(owner.Id);
                    break;
                case Outcome.Rejected:
                    application.ChangeStatus(
                        ApplicationStatus.Rejected, ApplicationActor.PetOwner, owner.Id,
                        "We were looking for a home with a garden. Thank you for applying.");
                    break;
                case Outcome.AutoRejected:
                    application.ChangeStatus(
                        ApplicationStatus.Rejected, ApplicationActor.PetOwner, owner.Id,
                        "Closed automatically: this pet has been adopted.");
                    break;
                case Outcome.Withdrawn:
                    application.ChangeStatus(
                        ApplicationStatus.Withdrawn, ApplicationActor.Applicant, applicant.Id, "Withdrawn by the applicant.");
                    break;
            }

            // Spread the later history entries over the following days so the timeline looks natural.
            for (var step = 1; step < application.History.Count; step++)
            {
                application.History[step].ChangedAtUtc = Clamp(created.AddHours(6 + (step - 1) * 20), now);
            }

            application.UpdatedAtUtc = application.History[^1].ChangedAtUtc;

            if (application.CompletedAtUtc is not null)
            {
                application.CompletedAtUtc = application.UpdatedAtUtc;
            }

            db.Applications.Add(application);
            AddNotifications(db, application, pet, applicant, owner, now);

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void AddNotifications(
        AppDbContext db, AdoptionApplication application, Pet pet, ApplicationUser applicant,
        ApplicationUser owner, DateTime now)
    {
        var verb = application.Type == ApplicationType.Adoption ? "adopt" : "foster";
        var kind = application.Type == ApplicationType.Adoption ? "adoption" : "foster";

        Add(owner.Id, NotificationType.ApplicationSubmitted, "New application",
            $"{applicant.DisplayName} applied to {verb} {pet.Name}.", application.CreatedAtUtc);

        for (var i = 1; i < application.History.Count; i++)
        {
            var entry = application.History[i];

            if (entry.ToStatus == ApplicationStatus.Withdrawn)
            {
                Add(owner.Id, NotificationType.ApplicationWithdrawn, "Application withdrawn",
                    $"{applicant.DisplayName} withdrew their application for {pet.Name}.", entry.ChangedAtUtc);
            }
            else if (entry.FromStatus == entry.ToStatus)
            {
                Add(applicant.Id, NotificationType.AdoptionCompleted, "Adoption completed",
                    $"The adoption of {pet.Name} has been completed. Congratulations!", entry.ChangedAtUtc);
            }
            else
            {
                Add(applicant.Id, NotificationType.ApplicationStatusChanged, "Application status changed",
                    $"Your {kind} application for {pet.Name} is now {entry.ToStatus}.", entry.ChangedAtUtc);
            }
        }

        void Add(Guid userId, NotificationType type, string title, string message, DateTime at)
        {
            var read = at < now.AddDays(-5);

            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Application = application,
                PetId = pet.Id,
                IsRead = read,
                ReadAtUtc = read ? at.AddHours(2) : null,
                CreatedAtUtc = at,
            });
        }
    }

    /* ---------- reports ---------- */

    private static async Task EnsureReportsAsync(
        AppDbContext db, Dictionary<string, ApplicationUser> accounts, Dictionary<string, Pet> pets,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var admin = accounts[AdminEmail];

        // An open report, waiting in the admin queue.
        if (pets.TryGetValue("Tiger", out var tiger))
        {
            var reporter = accounts[NadiaEmail];
            var tigerId = tiger.Id;
            var reporterId = reporter.Id;

            if (!await db.Reports.AnyAsync(r => r.PetId == tigerId && r.ReporterId == reporterId, cancellationToken))
            {
                db.Reports.Add(new Report
                {
                    PetId = tiger.Id,
                    ReporterId = reporter.Id,
                    Reason = ReportReason.Misleading,
                    Details = "The photos do not look like the animal described in the listing.",
                    CreatedAtUtc = now.AddDays(-1),
                });
            }
        }

        // A resolved report that led to the suspension of "Shadow".
        if (pets.TryGetValue("Shadow", out var shadow))
        {
            var reporter = accounts[OmarEmail];
            var shadowId = shadow.Id;
            var reporterId = reporter.Id;

            if (!await db.Reports.AnyAsync(r => r.PetId == shadowId && r.ReporterId == reporterId, cancellationToken))
            {
                const string note = "Photos did not match the animal. The listing was suspended.";

                var report = new Report
                {
                    PetId = shadow.Id,
                    ReporterId = reporter.Id,
                    Reason = ReportReason.Inappropriate,
                    Details = "The pictures seem to be from another website.",
                    CreatedAtUtc = now.AddDays(-4),
                };

                report.Review(ReportStatus.Resolved, admin.Id, note);
                report.ReviewedAtUtc = now.AddDays(-3);
                db.Reports.Add(report);

                db.Notifications.Add(new Notification
                {
                    UserId = shadow.OwnerId,
                    Type = NotificationType.ListingModerated,
                    Title = "Listing suspended",
                    Message = $"Your listing Shadow was suspended by moderation. Reason: {shadow.ModerationNote}",
                    PetId = shadow.Id,
                    CreatedAtUtc = now.AddDays(-3),
                });

                db.Notifications.Add(new Notification
                {
                    UserId = reporter.Id,
                    Type = NotificationType.ReportReviewed,
                    Title = "Report reviewed",
                    Message = "Your report about Shadow was reviewed: action was taken. Thank you for helping keep PawConnect safe.",
                    PetId = shadow.Id,
                    CreatedAtUtc = now.AddDays(-3),
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}