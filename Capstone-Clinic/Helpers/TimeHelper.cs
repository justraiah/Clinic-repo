
using System;

namespace Capstone_Clinic.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo PhilippineTime =
            GetPhilippineTimeZone();

        private static TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Singapore Standard Time");
            }
        }

        // Centralized Philippine local time
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                PhilippineTime);

        // Philippine local date
        public static DateTime Today => Now.Date;

        public static string GetTimeAgo(DateTime dateTime)
        {
            var span = Now - dateTime;

            if (span.TotalSeconds < 60)
                return "Just now";

            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} min ago";

            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hr ago";

            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} day(s) ago";

            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}
