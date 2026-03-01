using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCenter.Models;

/// <summary>
/// Фотография, прикрепленная к активности
/// </summary>
public class ActivityPhoto
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
    /// Пользователь, загрузивший фото
    /// </summary>
    [Required]
    public int UploadedByUserId { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public ApplicationUser UploadedByUser { get; set; } = null!;

    /// <summary>
    /// Путь к файлу
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Оригинальное имя файла
    /// </summary>
    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Размер файла в байтах
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME тип
    /// </summary>
    [MaxLength(100)]
    public string? ContentType { get; set; }

    /// <summary>
    /// Описание фото
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
