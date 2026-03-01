using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCenter.Models;

/// <summary>
/// Комментарий к активности
/// </summary>
public class ActivityComment
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
    /// Автор комментария
    /// </summary>
    [Required]
    public int AuthorUserId { get; set; }

    [ForeignKey(nameof(AuthorUserId))]
    public ApplicationUser AuthorUser { get; set; } = null!;

    /// <summary>
    /// Текст комментария
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Родительский комментарий (для ответов)
    /// </summary>
    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public ActivityComment? ParentComment { get; set; }

    /// <summary>
    /// Дочерние комментарии (ответы)
    /// </summary>
    public ICollection<ActivityComment> Replies { get; set; } = new List<ActivityComment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Удален ли комментарий
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
