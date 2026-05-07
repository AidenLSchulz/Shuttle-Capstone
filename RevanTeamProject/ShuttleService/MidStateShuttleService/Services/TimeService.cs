namespace MidStateShuttleService.Services
{
    /// <summary>
    /// Provides utility methods for converting DateTime values between UTC and Central Time.
    /// All timestamps should be stored in UTC and converted for display purposes only.
    /// </summary>
    /// <remarks>
    /// DEV NOTE: "Central Standard Time" is the Windows timezone ID and will not resolve correctly
    /// on Linux or macOS hosted environments (e.g. Docker, Azure Linux). Use "America/Chicago"
    /// with a cross-platform fallback if non-Windows hosting is ever introduced.
    /// </remarks>
    public static class TimeService
    {
        // Central Time zone — shared across all conversions to avoid repeated lookups.
        private static readonly TimeZoneInfo CentralZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        /// <summary>
        /// Converts a UTC <see cref="DateTime"/> to Central Time for display purposes.
        /// </summary>
        /// <param name="utcDate">The UTC date and time to convert.</param>
        /// <returns>The equivalent Central Time <see cref="DateTime"/>.</returns>
        public static DateTime ConvertUtcToCentral(DateTime utcDate)
        {
            // Explicitly mark the input as UTC before converting to prevent incorrect assumptions
            // about the DateTimeKind, which defaults to Unspecified if not set by the caller.
            var utc = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, CentralZone);
        }

        /// <summary>
        /// Converts a Central Time <see cref="DateTime"/> to UTC for storage.
        /// </summary>
        /// <param name="centralDate">The Central Time date and time to convert.</param>
        /// <returns>The equivalent UTC <see cref="DateTime"/>.</returns>
        public static DateTime ConvertCentralToUtc(DateTime centralDate)
        {
            // DateTimeKind must be Unspecified for ConvertTimeToUtc to treat the input as Central Time.
            var central = DateTime.SpecifyKind(centralDate, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(central, CentralZone);
        }
    }
}