using System.ComponentModel.DataAnnotations;

namespace HomeCenter.Models;

/// <summary>
/// Справочник видов деятельности
/// </summary>
public class ActivityType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Цвет для отображения в календаре (hex формат, например #FF5733)
    /// </summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#007bff";

    /// <summary>
    /// Активен ли тип деятельности
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Связанные активности
    /// </summary>
    public ICollection<ScheduledActivity> Activities { get; set; } = new List<ScheduledActivity>();
}
