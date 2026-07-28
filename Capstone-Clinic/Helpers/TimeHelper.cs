namespace Capstone_Clinic.Helpers
{
    public static class TimeHelper
    {
        public static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;

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