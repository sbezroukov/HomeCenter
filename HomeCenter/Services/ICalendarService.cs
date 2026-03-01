using HomeCenter.Models;

namespace HomeCenter.Services;

/// <summary>
/// Интерфейс сервиса для работы с календарем и активностями
/// </summary>
public interface ICalendarService
{
    // Activity Types
    Task<List<ActivityType>> GetAllActivityTypesAsync(bool includeInactive = false);
    Task<ActivityType?> GetActivityTypeByIdAsync(int id);
    Task<ActivityType> CreateActivityTypeAsync(ActivityType activityType);
    Task<ActivityType> UpdateActivityTypeAsync(ActivityType activityType);
    Task DeleteActivityTypeAsync(int id);

    // Scheduled Activities
    Task<List<ScheduledActivity>> GetActivitiesForWeekAsync(DateTime weekStart, int? userId = null);
    Task<List<ScheduledActivity>> GetActivitiesForDayAsync(DateTime date, int? userId = null);
    Task<ScheduledActivity?> GetActivityByIdAsync(int id);
    Task<ScheduledActivity> CreateActivityAsync(ScheduledActivity activity);
    Task<ScheduledActivity> UpdateActivityAsync(ScheduledActivity activity);
    Task DeleteActivityAsync(int id);
    Task<List<ScheduledActivity>> GetOverdueActivitiesAsync();
    Task<List<ScheduledActivity>> GetUpcomingActivitiesAsync(int hours = 1);

    // Activity Completions
    Task<ActivityCompletion> CompleteActivityAsync(int activityId, int userId, CompletionStatus status, string? comment = null);
    Task<ActivityCompletion?> GetActivityCompletionAsync(int activityId);
    Task<List<ActivityCompletion>> GetUserCompletionsAsync(int userId, DateTime? from = null, DateTime? to = null);

    // Statistics
    Task<Dictionary<string, int>> GetCompletionStatisticsAsync(int userId, DateTime from, DateTime to);
    Task<Dictionary<string, object>> GetDetailedStatisticsAsync(DateTime from, DateTime to, int? userId = null);

    // Copy and Move
    Task<List<ScheduledActivity>> CopyWeekScheduleAsync(DateTime sourceWeekStart, DateTime targetWeekStart, int userId);
    Task<List<ScheduledActivity>> CopyDayScheduleAsync(DateTime sourceDate, DateTime targetDate, int userId);
    Task MoveDayScheduleAsync(DateTime sourceDate, DateTime targetDate);

    // Photos and Comments
    Task<ActivityPhoto> AddPhotoAsync(int activityId, int userId, IFormFile file, string? description = null);
    Task<List<ActivityPhoto>> GetActivityPhotosAsync(int activityId);
    Task DeletePhotoAsync(int photoId);
    Task<ActivityComment> AddCommentAsync(int activityId, int userId, string text, int? parentCommentId = null);
    Task<List<ActivityComment>> GetActivityCommentsAsync(int activityId);
    Task DeleteCommentAsync(int commentId);

    // Supervisor Approval
    Task ApproveCompletionAsync(int completionId, int supervisorUserId);
    Task RejectCompletionAsync(int completionId, int supervisorUserId, string reason);

    // Test Integration
    Task<List<Topic>> GetAvailableTestsAsync();
    Task<TestAttempt?> GetTestResultForActivityAsync(int activityId, int userId);
    Task LinkTestAttemptToCompletionAsync(int completionId, int testAttemptId);
}
