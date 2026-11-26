using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data.Entities;

namespace NbcCms.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();

        await SeedRoles(db);
        await SeedStatuses(db);
        await SeedProgrammes(db);

        logger.LogInformation("NBC-CMS API database is ready.");
    }

    private static async Task SeedRoles(ApplicationDbContext db)
    {
        var definitions = new[]
        {
            (AppRoles.Artist, "External artist or claimer who submits and corrects claims."),
            (AppRoles.Secretary, "Internal user who reviews, corrects and approves claims."),
            (AppRoles.Admin, "System administrator with full access.")
        };

        var existing = await db.UserRoles.Select(r => r.RoleName).ToListAsync();
        var added = false;
        foreach (var (name, description) in definitions)
        {
            if (!existing.Contains(name))
            {
                db.UserRoles.Add(new UserRole { RoleName = name, Description = description });
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedStatuses(ApplicationDbContext db)
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
        var added = false;
        foreach (var (name, description) in definitions)
        {
            if (!existing.Contains(name))
            {
                db.ClaimStatusLookup.Add(new ClaimStatus { StatusName = name, Description = description });
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedProgrammes(ApplicationDbContext db)
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
}
