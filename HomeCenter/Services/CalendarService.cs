using HomeCenter.Data;
using HomeCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeCenter.Services;

/// <summary>
/// Сервис для работы с календарем и активностями
/// </summary>
public partial class CalendarService : ICalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(ApplicationDbContext context, ILogger<CalendarService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Activity Types

    public async Task<List<ActivityType>> GetAllActivityTypesAsync(bool includeInactive = false)
    {
        var query = _context.ActivityTypes.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(at => at.IsActive);
        }

        return await query.OrderBy(at => at.Name).ToListAsync();
    }

    public async Task<ActivityType?> GetActivityTypeByIdAsync(int id)
    {
        return await _context.ActivityTypes.FindAsync(id);
    }

    public async Task<ActivityType> CreateActivityTypeAsync(ActivityType activityType)
    {
        activityType.CreatedAt = DateTime.UtcNow;
        _context.ActivityTypes.Add(activityType);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created activity type: {Name} (ID: {Id})", activityType.Name, activityType.Id);
        return activityType;
    }

    public async Task<ActivityType> UpdateActivityTypeAsync(ActivityType activityType)
    {
        activityType.UpdatedAt = DateTime.UtcNow;
        _context.ActivityTypes.Update(activityType);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated activity type: {Name} (ID: {Id})", activityType.Name, activityType.Id);
        return activityType;
    }

    public async Task DeleteActivityTypeAsync(int id)
    {
        var activityType = await _context.ActivityTypes.FindAsync(id);
        if (activityType != null)
        {
            activityType.IsActive = false;
            activityType.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deactivated activity type: {Name} (ID: {Id})", activityType.Name, activityType.Id);
        }
    }

    #endregion

    #region Scheduled Activities

    public async Task<List<ScheduledActivity>> GetActivitiesForWeekAsync(DateTime weekStart, int? userId = null)
    {
        var weekEnd = weekStart.AddDays(7);

        var query = _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Include(sa => sa.AssignedToUser)
            .Include(sa => sa.CreatedByUser)
            .Include(sa => sa.Completions)
            .Where(sa => sa.IsActive && sa.StartDate >= weekStart && sa.StartDate < weekEnd);

        if (userId.HasValue)
        {
            query = query.Where(sa => sa.AssignedToUserId == userId || sa.AssignedToUserId == null);
        }

        return await query.OrderBy(sa => sa.StartDate).ThenBy(sa => sa.StartTime).ToListAsync();
    }

    public async Task<List<ScheduledActivity>> GetActivitiesForDayAsync(DateTime date, int? userId = null)
    {
        var dateOnly = date.Date;

        var query = _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Include(sa => sa.AssignedToUser)
            .Include(sa => sa.CreatedByUser)
            .Include(sa => sa.Completions)
            .Where(sa => sa.IsActive && sa.StartDate.Date == dateOnly);

        if (userId.HasValue)
        {
            query = query.Where(sa => sa.AssignedToUserId == userId || sa.AssignedToUserId == null);
        }

        return await query.OrderBy(sa => sa.StartTime).ToListAsync();
    }

    public async Task<ScheduledActivity?> GetActivityByIdAsync(int id)
    {
        return await _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Include(sa => sa.AssignedToUser)
            .Include(sa => sa.CreatedByUser)
            .Include(sa => sa.Completions)
            .FirstOrDefaultAsync(sa => sa.Id == id);
    }

    public async Task<ScheduledActivity> CreateActivityAsync(ScheduledActivity activity)
    {
        activity.CreatedAt = DateTime.UtcNow;
        _context.ScheduledActivities.Add(activity);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created scheduled activity: {Title} (ID: {Id})", activity.DisplayTitle, activity.Id);
        return activity;
    }

    public async Task<ScheduledActivity> UpdateActivityAsync(ScheduledActivity activity)
    {
        activity.UpdatedAt = DateTime.UtcNow;
        _context.ScheduledActivities.Update(activity);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated scheduled activity: {Title} (ID: {Id})", activity.DisplayTitle, activity.Id);
        return activity;
    }

    public async Task DeleteActivityAsync(int id)
    {
        var activity = await _context.ScheduledActivities.FindAsync(id);
        if (activity != null)
        {
            activity.IsActive = false;
            activity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deactivated scheduled activity: {Title} (ID: {Id})", activity.DisplayTitle, activity.Id);
        }
    }

    public async Task<List<ScheduledActivity>> GetOverdueActivitiesAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Include(sa => sa.AssignedToUser)
            .Include(sa => sa.Completions)
            .Where(sa => sa.IsActive 
                && sa.DeadlineDateTime.HasValue 
                && sa.DeadlineDateTime.Value < now
                && !sa.Completions.Any(c => c.Status == CompletionStatus.Completed))
            .OrderBy(sa => sa.DeadlineDateTime)
            .ToListAsync();
    }

    public async Task<List<ScheduledActivity>> GetUpcomingActivitiesAsync(int hours = 1)
    {
        var now = DateTime.UtcNow;
        var upcoming = now.AddHours(hours);

        var activities = await _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Include(sa => sa.AssignedToUser)
            .Include(sa => sa.Completions)
            .Where(sa => sa.IsActive 
                && sa.StartTime.HasValue
                && !sa.Completions.Any(c => c.Status == CompletionStatus.Completed))
            .ToListAsync();

        return activities
            .Where(sa =>
            {
                var activityDateTime = sa.StartDate.Date.Add(sa.StartTime!.Value);
                return activityDateTime >= now && activityDateTime <= upcoming;
            })
            .OrderBy(sa => sa.StartDate)
            .ThenBy(sa => sa.StartTime)
            .ToList();
    }

    #endregion

    #region Activity Completions

    public async Task<ActivityCompletion> CompleteActivityAsync(int activityId, int userId, CompletionStatus status, string? comment = null)
    {
        var activity = await GetActivityByIdAsync(activityId);
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity with ID {activityId} not found");
        }

        var isOnTime = !activity.DeadlineDateTime.HasValue || DateTime.UtcNow <= activity.DeadlineDateTime.Value;

        var completion = new ActivityCompletion
        {
            ScheduledActivityId = activityId,
            CompletedByUserId = userId,
            Status = status,
            Comment = comment,
            CompletedAt = DateTime.UtcNow,
            IsOnTime = isOnTime
        };

        _context.ActivityCompletions.Add(completion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activity {ActivityId} completed by user {UserId} with status {Status}", 
            activityId, userId, status);

        return completion;
    }

    public async Task<ActivityCompletion?> GetActivityCompletionAsync(int activityId)
    {
        return await _context.ActivityCompletions
            .Include(ac => ac.CompletedByUser)
            .Where(ac => ac.ScheduledActivityId == activityId)
            .OrderByDescending(ac => ac.CompletedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ActivityCompletion>> GetUserCompletionsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.ActivityCompletions
            .Include(ac => ac.ScheduledActivity)
                .ThenInclude(sa => sa.ActivityType)
            .Where(ac => ac.CompletedByUserId == userId);

        if (from.HasValue)
        {
            query = query.Where(ac => ac.CompletedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(ac => ac.CompletedAt <= to.Value);
        }

        return await query.OrderByDescending(ac => ac.CompletedAt).ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetCompletionStatisticsAsync(int userId, DateTime from, DateTime to)
    {
        var completions = await _context.ActivityCompletions
            .Where(ac => ac.CompletedByUserId == userId 
                && ac.CompletedAt >= from 
                && ac.CompletedAt <= to)
            .GroupBy(ac => ac.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return completions.ToDictionary(
            x => x.Status.ToString(),
            x => x.Count
        );
    }

    #endregion
}
