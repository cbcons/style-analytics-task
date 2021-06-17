using System;

namespace BadProject.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime DateTimeNow => DateTime.Now;
        public DateTimeOffset DateTimeOffsetNow => DateTimeOffset.Now;
    }
}