using System;

namespace ZeroUI.Core.Input.Date
{
    /// <summary>
    /// Quick selection date presets for industrial calendar and date range selectors.
    /// </summary>
    public enum DatePresetType
    {
        Today,
        Yesterday,
        Tomorrow,
        NextWeek,
        StartOfWeek,
        EndOfWeek,
        StartOfMonth,
        EndOfMonth
    }

    /// <summary>
    /// Helper utilities for evaluating date presets with zero heap allocations.
    /// </summary>
    public static class DatePresetHelper
    {
        /// <summary>
        /// Evaluates a date preset relative to a base date (defaults to DateTime.Today).
        /// </summary>
        public static DateTime Calculate(DatePresetType preset, DateTime? relativeTo = null)
        {
            DateTime baseDate = (relativeTo ?? DateTime.Today).Date;

            switch (preset)
            {
                case DatePresetType.Today:
                    return baseDate;

                case DatePresetType.Yesterday:
                    return baseDate.AddDays(-1);

                case DatePresetType.Tomorrow:
                    return baseDate.AddDays(1);

                case DatePresetType.NextWeek:
                    return baseDate.AddDays(7);

                case DatePresetType.StartOfWeek:
                    int diffToMonday = ((int)baseDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    return baseDate.AddDays(-diffToMonday);

                case DatePresetType.EndOfWeek:
                    int diffToSunday = ((int)DayOfWeek.Sunday - (int)baseDate.DayOfWeek + 7) % 7;
                    return baseDate.AddDays(diffToSunday);

                case DatePresetType.StartOfMonth:
                    return new DateTime(baseDate.Year, baseDate.Month, 1);

                case DatePresetType.EndOfMonth:
                    int daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
                    return new DateTime(baseDate.Year, baseDate.Month, daysInMonth);

                default:
                    return baseDate;
            }
        }
    }
}
