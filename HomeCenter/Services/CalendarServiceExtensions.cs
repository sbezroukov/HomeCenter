using HomeCenter.Data;
using HomeCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeCenter.Services;

/// <summary>
/// Расширения CalendarService для новых функций
/// </summary>
public partial class CalendarService
{
    #region Copy and Move

    public async Task<List<ScheduledActivity>> CopyWeekScheduleAsync(DateTime sourceWeekStart, DateTime targetWeekStart, int userId)
    {
        var sourceWeekEnd = sourceWeekStart.AddDays(7);
        var sourceActivities = await _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Where(sa => sa.IsActive 
                && sa.StartDate >= sourceWeekStart 
                && sa.StartDate < sourceWeekEnd)
            .ToListAsync();

        var copiedActivities = new List<ScheduledActivity>();
        var daysDiff = (targetWeekStart - sourceWeekStart).Days;

        foreach (var source in sourceActivities)
        {
            var copy = new ScheduledActivity
            {
                ActivityTypeId = source.ActivityTypeId,
                Title = source.Title,
                Description = source.Description,
                StartDate = source.StartDate.AddDays(daysDiff),
                StartTime = source.StartTime,
                EndTime = source.EndTime,
                DeadlineDateTime = source.DeadlineDateTime?.AddDays(daysDiff),
                AssignedToUserId = source.AssignedToUserId,
                CreatedByUserId = userId,
                IsRecurring = source.IsRecurring,
                RecurringDayOfWeek = source.RecurringDayOfWeek,
                TestTopicId = source.TestTopicId
            };

            _context.ScheduledActivities.Add(copy);
            copiedActivities.Add(copy);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Copied {Count} activities from {SourceDate} to {TargetDate}", 
            copiedActivities.Count, sourceWeekStart, targetWeekStart);

        return copiedActivities;
    }

    public async Task<List<ScheduledActivity>> CopyDayScheduleAsync(DateTime sourceDate, DateTime targetDate, int userId)
    {
        var sourceActivities = await _context.ScheduledActivities
            .Include(sa => sa.ActivityType)
            .Where(sa => sa.IsActive && sa.StartDate.Date == sourceDate.Date)
            .ToListAsync();

        var copiedActivities = new List<ScheduledActivity>();
        var daysDiff = (targetDate - sourceDate).Days;

        foreach (var source in sourceActivities)
        {
            var copy = new ScheduledActivity
            {
                ActivityTypeId = source.ActivityTypeId,
                Title = source.Title,
                Description = source.Description,
                StartDate = targetDate.Date,
                StartTime = source.StartTime,
                EndTime = source.EndTime,
                DeadlineDateTime = source.DeadlineDateTime?.AddDays(daysDiff),
                AssignedToUserId = source.AssignedToUserId,
                CreatedByUserId = userId,
                IsRecurring = source.IsRecurring,
                RecurringDayOfWeek = source.RecurringDayOfWeek,
                TestTopicId = source.TestTopicId
            };

            _context.ScheduledActivities.Add(copy);
            copiedActivities.Add(copy);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Copied {Count} activities from {SourceDate} to {TargetDate}", 
            copiedActivities.Count, sourceDate, targetDate);

        return copiedActivities;
    }

    public async Task MoveDayScheduleAsync(DateTime sourceDate, DateTime targetDate)
    {
        var activities = await _context.ScheduledActivities
            .Where(sa => sa.IsActive && sa.StartDate.Date == sourceDate.Date)
            .ToListAsync();

        var daysDiff = (targetDate - sourceDate).Days;

        foreach (var activity in activities)
        {
            activity.StartDate = targetDate.Date;
            if (activity.DeadlineDateTime.HasValue)
            {
                activity.DeadlineDateTime = activity.DeadlineDateTime.Value.AddDays(daysDiff);
            }
            activity.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Moved {Count} activities from {SourceDate} to {TargetDate}", 
            activities.Count, sourceDate, targetDate);
    }

    #endregion

    #region Photos

    public async Task<ActivityPhoto> AddPhotoAsync(int activityId, int userId, IFormFile file, string? description = null)
    {
        var activity = await _context.ScheduledActivities.FindAsync(activityId);
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity {activityId} not found");
        }

        // Создаем директорию для фото, если её нет
        var uploadsDir = Path.Combine("wwwroot", "uploads", "activity-photos");
        Directory.CreateDirectory(uploadsDir);

        // Генерируем уникальное имя файла
        var fileExtension = Path.GetExtension(file.FileName);
        var fileName = $"{activityId}_{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        // Сохраняем файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var photo = new ActivityPhoto
        {
            ScheduledActivityId = activityId,
            UploadedByUserId = userId,
            FilePath = $"/uploads/activity-photos/{fileName}",
            OriginalFileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            Description = description
        };

        _context.ActivityPhotos.Add(photo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added photo to activity {ActivityId}", activityId);
        return photo;
    }

    public async Task<List<ActivityPhoto>> GetActivityPhotosAsync(int activityId)
    {
        return await _context.ActivityPhotos
            .Include(ap => ap.UploadedByUser)
            .Where(ap => ap.ScheduledActivityId == activityId)
            .OrderByDescending(ap => ap.UploadedAt)
            .ToListAsync();
    }

    public async Task DeletePhotoAsync(int photoId)
    {
        var photo = await _context.ActivityPhotos.FindAsync(photoId);
        if (photo != null)
        {
            // Удаляем файл с диска
            var fullPath = Path.Combine("wwwroot", photo.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            _context.ActivityPhotos.Remove(photo);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted photo {PhotoId}", photoId);
        }
    }

    #endregion

    #region Comments

    public async Task<ActivityComment> AddCommentAsync(int activityId, int userId, string text, int? parentCommentId = null)
    {
        var activity = await _context.ScheduledActivities.FindAsync(activityId);
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity {activityId} not found");
        }

        var comment = new ActivityComment
        {
            ScheduledActivityId = activityId,
            AuthorUserId = userId,
            Text = text,
            ParentCommentId = parentCommentId
        };

        _context.ActivityComments.Add(comment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added comment to activity {ActivityId}", activityId);
        return comment;
    }

    public async Task<List<ActivityComment>> GetActivityCommentsAsync(int activityId)
    {
        return await _context.ActivityComments
            .Include(ac => ac.AuthorUser)
            .Include(ac => ac.Replies)
                .ThenInclude(r => r.AuthorUser)
            .Where(ac => ac.ScheduledActivityId == activityId && !ac.IsDeleted && ac.ParentCommentId == null)
            .OrderBy(ac => ac.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _context.ActivityComments.FindAsync(commentId);
        if (comment != null)
        {
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted comment {CommentId}", commentId);
        }
    }

    #endregion

    #region Supervisor Approval

    public async Task ApproveCompletionAsync(int completionId, int supervisorUserId)
    {
        var completion = await _context.ActivityCompletions.FindAsync(completionId);
        if (completion == null)
        {
            throw new InvalidOperationException($"Completion {completionId} not found");
        }

        completion.IsApprovedBySupervisor = true;
        completion.ApprovedByUserId = supervisorUserId;
        completion.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Completion {CompletionId} approved by supervisor {SupervisorId}", 
            completionId, supervisorUserId);
    }

    public async Task RejectCompletionAsync(int completionId, int supervisorUserId, string reason)
    {
        var completion = await _context.ActivityCompletions.FindAsync(completionId);
        if (completion == null)
        {
            throw new InvalidOperationException($"Completion {completionId} not found");
        }

        completion.IsApprovedBySupervisor = false;
        completion.Comment = $"[ОТКЛОНЕНО] {reason}\n\n{completion.Comment}";
        completion.ApprovedByUserId = supervisorUserId;
        completion.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Completion {CompletionId} rejected by supervisor {SupervisorId}", 
            completionId, supervisorUserId);
    }

    #endregion

    #region Statistics

    public async Task<Dictionary<string, object>> GetDetailedStatisticsAsync(DateTime from, DateTime to, int? userId = null)
    {
        var query = _context.ActivityCompletions
            .Include(ac => ac.ScheduledActivity)
                .ThenInclude(sa => sa.ActivityType)
            .Include(ac => ac.CompletedByUser)
            .Where(ac => ac.CompletedAt >= from && ac.CompletedAt <= to);

        if (userId.HasValue)
        {
            query = query.Where(ac => ac.CompletedByUserId == userId.Value);
        }

        var completions = await query.ToListAsync();

        var stats = new Dictionary<string, object>
        {
            ["TotalTasks"] = completions.Count,
            ["CompletedTasks"] = completions.Count(c => c.Status == CompletionStatus.Completed),
            ["NotCompletedTasks"] = completions.Count(c => c.Status == CompletionStatus.NotCompleted),
            ["PartiallyCompletedTasks"] = completions.Count(c => c.Status == CompletionStatus.PartiallyCompleted),
            ["CancelledTasks"] = completions.Count(c => c.Status == CompletionStatus.Cancelled),
            ["OnTimeTasks"] = completions.Count(c => c.IsOnTime),
            ["LateTasks"] = completions.Count(c => !c.IsOnTime),
            ["ApprovedTasks"] = completions.Count(c => c.IsApprovedBySupervisor),
            ["PendingApprovalTasks"] = completions.Count(c => !c.IsApprovedBySupervisor && c.Status == CompletionStatus.Completed)
        };

        // Статистика по типам деятельности
        var byActivityType = completions
            .GroupBy(c => c.ScheduledActivity.ActivityType.Name)
            .Select(g => new
            {
                ActivityType = g.Key,
                Total = g.Count(),
                Completed = g.Count(c => c.Status == CompletionStatus.Completed)
            })
            .ToList();

        stats["ByActivityType"] = byActivityType;

        // Статистика по пользователям (если не указан конкретный пользователь)
        if (!userId.HasValue)
        {
            var byUser = completions
                .GroupBy(c => c.CompletedByUser.UserName)
                .Select(g => new
                {
                    UserName = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(c => c.Status == CompletionStatus.Completed),
                    CompletionRate = g.Count() > 0 ? (double)g.Count(c => c.Status == CompletionStatus.Completed) / g.Count() * 100 : 0
                })
                .OrderByDescending(x => x.CompletionRate)
                .ToList();

            stats["ByUser"] = byUser;
        }

        return stats;
    }

    #endregion

    #region Test Integration

    public async Task<List<Topic>> GetAvailableTestsAsync()
    {
        return await _context.Topics
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Title)
            .ToListAsync();
    }

    public async Task<TestAttempt?> GetTestResultForActivityAsync(int activityId, int userId)
    {
        var activity = await _context.ScheduledActivities
            .Include(sa => sa.Completions)
                .ThenInclude(c => c.TestAttempt)
            .FirstOrDefaultAsync(sa => sa.Id == activityId);

        if (activity == null || !activity.TestTopicId.HasValue)
        {
            return null;
        }

        // Ищем последнюю попытку теста для этого пользователя
        var completion = activity.Completions
            .Where(c => c.CompletedByUserId == userId && c.TestAttempt != null)
            .OrderByDescending(c => c.CompletedAt)
            .FirstOrDefault();

        return completion?.TestAttempt;
    }

    public async Task LinkTestAttemptToCompletionAsync(int completionId, int testAttemptId)
    {
        var completion = await _context.ActivityCompletions.FindAsync(completionId);
        if (completion == null)
        {
            throw new InvalidOperationException($"Completion {completionId} not found");
        }

        var testAttempt = await _context.Attempts.FindAsync(testAttemptId);
        if (testAttempt == null)
        {
            throw new InvalidOperationException($"TestAttempt {testAttemptId} not found");
        }

        completion.TestAttemptId = testAttemptId;

        // Определяем статус на основе результата теста
        var successThreshold = 95.0;
        var scorePercentage = testAttempt.ScorePercent ?? 0;

        if (scorePercentage >= successThreshold)
        {
            completion.Status = CompletionStatus.Completed;
        }
        else if (scorePercentage >= 50)
        {
            completion.Status = CompletionStatus.PartiallyCompleted;
        }
        else
        {
            completion.Status = CompletionStatus.NotCompleted;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Linked test attempt {TestAttemptId} to completion {CompletionId}", 
            testAttemptId, completionId);
    }

    #endregion
}
