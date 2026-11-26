using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data.Entities;
using NbcCms.Web.Services;

namespace NbcCms.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CmsDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();

        await SeedRoles(db);
        await SeedStatuses(db);
        await SeedProgrammes(db);
        await SeedUsers(db, hasher);
        await SeedClaims(db);

        logger.LogInformation("NBC-CMS database is ready.");
    }

    private static async Task SeedRoles(CmsDbContext db)
    {
        var definitions = new[]
        {
            (RoleNames.Artist, "External artist or claimer who submits and corrects claims."),
            (RoleNames.Secretary, "Internal user who reviews, corrects and approves claims."),
            (RoleNames.Admin, "System administrator with full access.")
        };

        var existing = await db.UserRoles.Select(r => r.RoleName).ToListAsync();
        foreach (var (name, description) in definitions)
        {
            if (!existing.Contains(name))
            {
                db.UserRoles.Add(new UserRole { RoleName = name, Description = description });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedStatuses(CmsDbContext db)
    {
        var definitions = new[]
        {
            (ClaimStatusNames.Draft, "Claim is being prepared and has not been submitted."),
            (ClaimStatusNames.Submitted, "Claim has been submitted and is awaiting review."),
            (ClaimStatusNames.CorrectionRequested, "The Secretary identified an error or missing information and requested corrections."),
            (ClaimStatusNames.Resubmitted, "The Artist corrected the claim and submitted it again."),
            (ClaimStatusNames.Accepted, "The Secretary approved the claim."),
            (ClaimStatusNames.Declined, "The Secretary declined the claim.")
        };

        var existing = await db.ClaimStatusLookup.Select(s => s.StatusName).ToListAsync();
        foreach (var (name, description) in definitions)
        {
            if (!existing.Contains(name))
            {
                db.ClaimStatusLookup.Add(new ClaimStatus { StatusName = name, Description = description });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedProgrammes(CmsDbContext db)
    {
        if (await db.Programmes.AnyAsync())
        {
            return;
        }

        var programmes = new[]
        {
            ("Prime News Tonight", "Flagship evening news bulletin."),
            ("The Morning Show", "Weekday morning magazine programme."),
            ("Namibia Today", "Daily current affairs programme."),
            ("Sports Desk", "Weekend sports wrap and live analysis."),
            ("One Africa", "Continental affairs and culture."),
            ("NBC Drama Series", "Locally produced drama productions."),
            ("Fresh FM Sessions", "Live music sessions recorded for radio."),
            ("National Events Coverage", "Official ceremonies and national events.")
        };

        db.Programmes.AddRange(programmes.Select(p => new Programme
        {
            ProgrammeName = p.Item1,
            Description = p.Item2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }));

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsers(CmsDbContext db, IPasswordHasher hasher)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var roles = await db.UserRoles.ToDictionaryAsync(r => r.RoleName);
        var now = DateTime.UtcNow;

        var secretary = new User
        {
            Username = "secretary",
            FirstName = "Maria",
            LastName = "Nghipondoka",
            Email = "m.nghipondoka@nbc.com.na",
            ContactNumber = "+264 61 291 3111",
            RoleId = roles[RoleNames.Secretary].RoleId,
            IsActive = true,
            CreatedAt = now,
            PasswordHash = hasher.Hash("Secretary@123")
        };

        var artist = new User
        {
            Username = "artist.demo",
            FirstName = "Tuyakula",
            LastName = "Nangolo",
            Email = "t.nangolo@example.com",
            ContactNumber = "+264 81 234 5678",
            RoleId = roles[RoleNames.Artist].RoleId,
            IsActive = true,
            CreatedAt = now,
            IdNumber = "90041200123",
            Address = "12 Bach Street, Windhoek",
            PasswordHash = hasher.Hash("Artist@123")
        };

        var secondArtist = new User
        {
            Username = "artist.selma",
            FirstName = "Selma",
            LastName = "Kandjii",
            Email = "s.kandjii@example.com",
            ContactNumber = "+264 85 765 4321",
            RoleId = roles[RoleNames.Artist].RoleId,
            IsActive = true,
            CreatedAt = now,
            IdNumber = "94082100456",
            Address = "4 Ombika Street, Katutura",
            PasswordHash = hasher.Hash("Artist@123")
        };

        var admin = new User
        {
            Username = "admin",
            FirstName = "System",
            LastName = "Administrator",
            Email = "cms.admin@nbc.com.na",
            RoleId = roles[RoleNames.Admin].RoleId,
            IsActive = true,
            CreatedAt = now,
            PasswordHash = hasher.Hash("Admin@123")
        };

        db.Users.AddRange(secretary, artist, secondArtist, admin);
        await db.SaveChangesAsync();

        db.ArtistBankingDetails.Add(new ArtistBankingDetail
        {
            ArtistId = artist.UserId,
            Bank = "Bank Windhoek",
            Branch = "Klein Windhoek",
            BranchCode = "481-172",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = secretary.UserId,
            UpdatedBy = secretary.UserId
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedClaims(CmsDbContext db)
    {
        if (await db.Claims.AnyAsync())
        {
            return;
        }

        var artist = await db.Users.FirstAsync(u => u.Username == "artist.demo");
        var secretary = await db.Users.FirstAsync(u => u.Username == "secretary");
        var statuses = await db.ClaimStatusLookup.ToDictionaryAsync(s => s.StatusName);
        var now = DateTime.UtcNow;
        var today = DateTime.Today;

        var accepted = new Claim
        {
            ClaimantId = artist.UserId,
            StatusId = statuses[ClaimStatusNames.Accepted].StatusId,
            ClaimDate = today.AddDays(-9),
            SubmittedAt = now.AddDays(-8),
            ReviewedBy = secretary.UserId,
            ReviewedAt = now.AddDays(-6),
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-6),
            RecordingDate = today.AddDays(-12),
            ProgrammeTitle = "Prime News Tonight, The Morning Show",
            FeesAmount = 2700m,
            Items =
            {
                new ClaimItem { RecordingDate = today.AddDays(-12), ProgrammeTitle = "Prime News Tonight", Rate = 1500m, FeesAmount = 1500m, CreatedAt = now.AddDays(-10) },
                new ClaimItem { RecordingDate = today.AddDays(-11), ProgrammeTitle = "The Morning Show", Rate = 1200m, FeesAmount = 1200m, CreatedAt = now.AddDays(-10) }
            }
        };

        var submitted = new Claim
        {
            ClaimantId = artist.UserId,
            StatusId = statuses[ClaimStatusNames.Submitted].StatusId,
            ClaimDate = today.AddDays(-2),
            SubmittedAt = now.AddDays(-2),
            CreatedAt = now.AddDays(-3),
            RecordingDate = today.AddDays(-4),
            ProgrammeTitle = "Sports Desk",
            FeesAmount = 1800m,
            Items =
            {
                new ClaimItem { RecordingDate = today.AddDays(-4), ProgrammeTitle = "Sports Desk", Rate = 1800m, FeesAmount = 1800m, CreatedAt = now.AddDays(-3) }
            }
        };

        var correction = new Claim
        {
            ClaimantId = artist.UserId,
            StatusId = statuses[ClaimStatusNames.CorrectionRequested].StatusId,
            ClaimDate = today.AddDays(-6),
            SubmittedAt = now.AddDays(-6),
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-4),
            RecordingDate = today.AddDays(-8),
            ProgrammeTitle = "NBC Drama Series",
            FeesAmount = 2200m,
            Items =
            {
                new ClaimItem { RecordingDate = today.AddDays(-8), ProgrammeTitle = "NBC Drama Series", Rate = 2200m, FeesAmount = 2200m, CreatedAt = now.AddDays(-7) }
            }
        };

        var draft = new Claim
        {
            ClaimantId = artist.UserId,
            StatusId = statuses[ClaimStatusNames.Draft].StatusId,
            ClaimDate = today.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            RecordingDate = today.AddDays(-3),
            ProgrammeTitle = "One Africa",
            FeesAmount = 950m,
            Items =
            {
                new ClaimItem { RecordingDate = today.AddDays(-3), ProgrammeTitle = "One Africa", Rate = 950m, FeesAmount = 950m, CreatedAt = now.AddDays(-1) }
            }
        };

        db.Claims.AddRange(accepted, submitted, correction, draft);
        await db.SaveChangesAsync();

        db.ClaimCorrections.Add(new ClaimCorrection
        {
            ClaimId = correction.ClaimId,
            RequestedBy = secretary.UserId,
            CorrectionComment = "The recording date on the drama insert does not match the production schedule. Please correct the date and attach the studio booking confirmation.",
            RequestedAt = now.AddDays(-4)
        });

        await db.SaveChangesAsync();
    }
}
