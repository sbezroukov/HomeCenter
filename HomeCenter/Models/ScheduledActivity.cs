using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCenter.Models;

/// <summary>
/// Запланированная активность в календаре
/// </summary>
public class ScheduledActivity
{
    public int Id { get; set; }

    /// <summary>
    /// Тип деятельности
    /// </summary>
    [Required]
    public int ActivityTypeId { get; set; }

    [ForeignKey(nameof(ActivityTypeId))]
    public ActivityType ActivityType { get; set; } = null!;

    /// <summary>
    /// Название задачи (опционально, если отличается от типа)
    /// </summary>
    [MaxLength(300)]
    public string? Title { get; set; }

    /// <summary>
    /// Описание задачи
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала (обязательно)
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Время начала (если null - задача на весь день)
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Время окончания (если null - задача без конкретного времени окончания)
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Дата и время дедлайна для завершения задачи
    /// </summary>
    public DateTime? DeadlineDateTime { get; set; }

    /// <summary>
    /// Ответственный за выполнение (если null - задача для всех или не назначена)
    /// </summary>
    public int? AssignedToUserId { get; set; }

    [ForeignKey(nameof(AssignedToUserId))]
    public ApplicationUser? AssignedToUser { get; set; }

    /// <summary>
    /// Создатель задачи
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Повторяющаяся задача (еженедельно)
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// День недели для повторяющейся задачи (0 = воскресенье, 6 = суббота)
    /// </summary>
    public int? RecurringDayOfWeek { get; set; }

    /// <summary>
    /// Активна ли задача
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Связь с тестом (если задание - это тест)
    /// </summary>
    public int? TestTopicId { get; set; }

    [ForeignKey(nameof(TestTopicId))]
    public Topic? TestTopic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Отметки о выполнении
    /// </summary>
    public ICollection<ActivityCompletion> Completions { get; set; } = new List<ActivityCompletion>();

    /// <summary>
    /// Фотографии, прикрепленные к активности
    /// </summary>
    public ICollection<ActivityPhoto> Photos { get; set; } = new List<ActivityPhoto>();

    /// <summary>
    /// Комментарии к активности
    /// </summary>
    public ICollection<ActivityComment> Comments { get; set; } = new List<ActivityComment>();

    /// <summary>
    /// Является ли задача на весь день
    /// </summary>
    [NotMapped]
    public bool IsAllDay => !StartTime.HasValue;

    /// <summary>
    /// Отображаемое название (Title или название типа)
    /// </summary>
    [NotMapped]
    public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : ActivityType?.Name ?? "Без названия";
}
