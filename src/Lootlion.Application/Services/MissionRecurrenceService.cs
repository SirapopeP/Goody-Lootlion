using Lootlion.Application.Abstractions;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;

namespace Lootlion.Application.Services;

public sealed class MissionRecurrenceService : IMissionRecurrenceService
{
    public bool HasRecurrence(MissionTemplate template) =>
        template.RecurrenceKind != MissionRecurrenceKind.None;

    public string BuildPeriodKey(MissionTemplate template, Household household, DateTime utcNow)
    {
        var tz = ResolveTimeZone(household.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        return template.RecurrenceKind switch
        {
            MissionRecurrenceKind.None => $"once-{template.Id:N}",
            MissionRecurrenceKind.Daily => local.ToString("yyyy-MM-dd"),
            MissionRecurrenceKind.Weekly => $"{local:yyyy}-W{GetIsoWeek(local):D2}",
            MissionRecurrenceKind.Monthly => $"{local:yyyy-MM}",
            MissionRecurrenceKind.IntervalDays => $"interval-{template.Id:N}-{local:yyyyMMdd}",
            _ => $"once-{template.Id:N}"
        };
    }

    public string BuildNextPeriodKey(MissionTemplate template, Household household, string currentPeriodKey, DateTime completedUtc)
    {
        if (!HasRecurrence(template))
            throw new InvalidOperationException("Template has no recurrence.");

        var tz = ResolveTimeZone(household.TimeZoneId);
        var localCompleted = TimeZoneInfo.ConvertTimeFromUtc(completedUtc, tz);

        return template.RecurrenceKind switch
        {
            MissionRecurrenceKind.Daily => localCompleted.AddDays(1).ToString("yyyy-MM-dd"),
            MissionRecurrenceKind.Weekly => BuildWeeklyNext(localCompleted, template.RecurrenceDayOfWeek),
            MissionRecurrenceKind.Monthly => BuildMonthlyNext(localCompleted, template.RecurrenceDayOfMonth),
            MissionRecurrenceKind.IntervalDays => BuildIntervalNext(template, localCompleted),
            _ => throw new InvalidOperationException("Unsupported recurrence kind.")
        };
    }

    public DateTime ComputeAvailableFromUtc(MissionTemplate template, Household household, string periodKey)
    {
        var tz = ResolveTimeZone(household.TimeZoneId);

        if (template.RecurrenceKind == MissionRecurrenceKind.None)
            return DateTime.UtcNow;

        if (template.RecurrenceKind == MissionRecurrenceKind.Daily
            && DateTime.TryParseExact(periodKey, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var day))
        {
            var localMidnight = DateTime.SpecifyKind(day, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, tz);
        }

        return DateTime.UtcNow;
    }

    private static string BuildWeeklyNext(DateTime localCompleted, DayOfWeek? targetDay)
    {
        var day = targetDay ?? DayOfWeek.Monday;
        var daysUntil = ((int)day - (int)localCompleted.DayOfWeek + 7) % 7;
        if (daysUntil == 0)
            daysUntil = 7;
        var next = localCompleted.Date.AddDays(daysUntil);
        return $"{next:yyyy}-W{GetIsoWeek(next):D2}";
    }

    private static string BuildMonthlyNext(DateTime localCompleted, int? dayOfMonth)
    {
        var targetDay = Math.Clamp(dayOfMonth ?? 1, 1, 28);
        var nextMonth = localCompleted.Date.AddMonths(1);
        var next = new DateTime(nextMonth.Year, nextMonth.Month, targetDay);
        return next.ToString("yyyy-MM");
    }

    private static string BuildIntervalNext(MissionTemplate template, DateTime localCompleted)
    {
        var days = template.RecurrenceIntervalDays ?? 7;
        var next = localCompleted.Date.AddDays(days);
        return $"interval-{template.Id:N}-{next:yyyyMMdd}";
    }

    private static int GetIsoWeek(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
    }
}
