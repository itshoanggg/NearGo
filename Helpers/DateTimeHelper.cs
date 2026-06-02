namespace NearGo.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTz =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTz);
        }
    }
}
