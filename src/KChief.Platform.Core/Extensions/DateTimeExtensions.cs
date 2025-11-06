namespace KChief.Platform.Core.Extensions;

/// <summary>
/// Extension methods for DateTime operations.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    public static long ToUnixTimeSeconds(this DateTime dateTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
    }

    /// <summary>
    /// Converts DateTime to Unix timestamp (milliseconds since epoch).
    /// </summary>
    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dateTime.ToUniversalTime() - epoch).TotalMilliseconds;
    }

    /// <summary>
    /// Converts Unix timestamp (seconds) to DateTime.
    /// </summary>
    public static DateTime FromUnixTimeSeconds(long seconds)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(seconds);
    }

    /// <summary>
    /// Converts Unix timestamp (milliseconds) to DateTime.
    /// </summary>
    public static DateTime FromUnixTimeMilliseconds(long milliseconds)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddMilliseconds(milliseconds);
    }

    /// <summary>
    /// Gets the start of the day (00:00:00).
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999).
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Gets the end of the week (Sunday).
    /// </summary>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        return dateTime.StartOfWeek(startOfWeek).AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month)).EndOfDay();
    }

    /// <summary>
    /// Checks if a DateTime is within a time range.
    /// </summary>
    public static bool IsWithin(this DateTime dateTime, DateTime start, DateTime end)
    {
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "2 hours ago").
    /// </summary>
    public static string ToRelativeTimeString(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
        {
            return "just now";
        }

        if (timeSpan.TotalMinutes < 60)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (timeSpan.TotalHours < 24)
        {
            var hours = (int)timeSpan.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (timeSpan.TotalDays < 30)
        {
            var days = (int)timeSpan.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }

        if (timeSpan.TotalDays < 365)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }

        var years = (int)(timeSpan.TotalDays / 365);
        return $"{years} year{(years == 1 ? "" : "s")} ago";
    }

    /// <summary>
    /// Gets a formatted duration string (e.g., "2h 30m").
    /// </summary>
    public static string ToDurationString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }

        if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }

        if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }

        return $"{timeSpan.Seconds}s";
    }
}

