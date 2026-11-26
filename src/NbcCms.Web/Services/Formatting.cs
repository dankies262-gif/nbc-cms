using System.Globalization;

namespace NbcCms.Web.Services;

public static class Formatting
{
    private static readonly TimeZoneInfo Windhoek = Resolve();

    public static string Money(decimal? amount) =>
        amount.HasValue ? $"N$ {amount.Value.ToString("N2", CultureInfo.InvariantCulture)}" : "—";

    public static string Date(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "—";

    public static string Date(DateTime value) => value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string Timestamp(DateTime? utc) =>
        utc.HasValue ? ToWindhoek(utc.Value).ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture) : "—";

    public static DateTime ToWindhoek(DateTime utc)
    {
        var value = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(value, Windhoek);
    }

    public static string Ago(DateTime? utc)
    {
        if (!utc.HasValue)
        {
            return "—";
        }

        var span = DateTime.UtcNow - utc.Value;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} d ago";
        return Date(ToWindhoek(utc.Value));
    }

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Africa/Windhoek", "South Africa Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("NBC-Windhoek", TimeSpan.FromHours(2), "Windhoek", "Windhoek");
    }
}
