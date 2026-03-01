using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCenter.Models;

/// <summary>
/// Отметка о выполнении задачи
/// </summary>
public class ActivityCompletion
{
    public int Id { get; set; }

    /// <summary>
    /// Связанная активность
    /// </summary>
    [Required]
    public int ScheduledActivityId { get; set; }

    [ForeignKey(nameof(ScheduledActivityId))]
    public ScheduledActivity ScheduledActivity { get; set; } = null!;

    /// <summary>
    /// Пользователь, который отметил выполнение
    /// </summary>
    [Required]
    public int CompletedByUserId { get; set; }

    [ForeignKey(nameof(CompletedByUserId))]
    public ApplicationUser CompletedByUser { get; set; } = null!;

    /// <summary>
    /// Статус выполнения
    /// </summary>
    [Required]
    public CompletionStatus Status { get; set; }

    /// <summary>
    /// Причина невыполнения или комментарий
    /// </summary>
    [MaxLength(1000)]
    public string? Comment { get; set; }

    /// <summary>
    /// Дата и время отметки
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Была ли задача выполнена в срок
    /// </summary>
    public bool IsOnTime { get; set; }

    /// <summary>
    /// Подтверждено ли выполнение супервизором
    /// </summary>
    public bool IsApprovedBySupervisor { get; set; } = false;

    /// <summary>
    /// Кто подтвердил выполнение (Supervisor)
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    [ForeignKey(nameof(ApprovedByUserId))]
    public ApplicationUser? ApprovedByUser { get; set; }

    /// <summary>
    /// Дата и время подтверждения
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Результат теста (если задание было тестом)
    /// </summary>
    public int? TestAttemptId { get; set; }

    [ForeignKey(nameof(TestAttemptId))]
    public TestAttempt? TestAttempt { get; set; }
}

/// <summary>
/// Статус выполнения задачи
/// </summary>
public enum CompletionStatus
{
    /// <summary>
    /// Выполнена успешно
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Не выполнена
    /// </summary>
    NotCompleted = 2,

    /// <summary>
    /// Частично выполнена
    /// </summary>
    PartiallyCompleted = 3,

    /// <summary>
    /// Отменена
    /// </summary>
    Cancelled = 4
}
