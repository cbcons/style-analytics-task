using System;

namespace BadProject.Services
{
    public interface IDateTimeProvider
    {
        DateTime DateTimeNow { get; }
        DateTimeOffset DateTimeOffsetNow { get; }
    }
}