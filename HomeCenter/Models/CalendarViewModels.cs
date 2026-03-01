using System.ComponentModel.DataAnnotations;

namespace HomeCenter.Models;

/// <summary>
/// ViewModel для недельного представления календаря
/// </summary>
public class WeekCalendarViewModel
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<DayScheduleViewModel> Days { get; set; } = new();
    public List<ActivityType> AvailableActivityTypes { get; set; } = new();
    public List<ApplicationUser> AvailableUsers { get; set; } = new();
}

/// <summary>
/// ViewModel для дня в календаре
/// </summary>
public class DayScheduleViewModel
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public List<ScheduledActivityViewModel> Activities { get; set; } = new();
}

/// <summary>
/// ViewModel для отображения активности
/// </summary>
public class ScheduledActivityViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string ActivityTypeColor { get; set; } = "#007bff";
    public DateTime StartDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? DeadlineDateTime { get; set; }
    public string? AssignedToUserName { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsCompleted { get; set; }
    public CompletionStatus? CompletionStatus { get; set; }
    public bool IsOverdue { get; set; }
    public string? CompletionComment { get; set; }
}

/// <summary>
/// ViewModel для создания/редактирования активности
/// </summary>
public class CreateEditActivityViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Выберите тип деятельности")]
    [Display(Name = "Тип деятельности")]
    public int ActivityTypeId { get; set; }

    [MaxLength(300)]
    [Display(Name = "Название (опционально)")]
    public string? Title { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Укажите дату начала")]
    [Display(Name = "Дата начала")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Время начала (оставьте пустым для задачи на весь день)")]
    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    [Display(Name = "Время окончания")]
    [DataType(DataType.Time)]
    public TimeSpan? EndTime { get; set; }

    [Display(Name = "Дедлайн для завершения")]
    [DataType(DataType.DateTime)]
    public DateTime? DeadlineDateTime { get; set; }

    [Display(Name = "Ответственный")]
    public int? AssignedToUserId { get; set; }

    [Display(Name = "Повторяющаяся задача (еженедельно)")]
    public bool IsRecurring { get; set; }

    [Display(Name = "День недели для повторения")]
    public int? RecurringDayOfWeek { get; set; }

    [Display(Name = "Связанный тест (если задание - это тест)")]
    public int? TestTopicId { get; set; }

    public List<ActivityType> AvailableActivityTypes { get; set; } = new();
    public List<ApplicationUser> AvailableUsers { get; set; } = new();
    public List<Topic> AvailableTests { get; set; } = new();
}

/// <summary>
/// ViewModel для отметки выполнения
/// </summary>
public class CompleteActivityViewModel
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите статус выполнения")]
    [Display(Name = "Статус")]
    public CompletionStatus Status { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Комментарий / Причина невыполнения")]
    public string? Comment { get; set; }
}

/// <summary>
/// ViewModel для управления типами деятельности
/// </summary>
public class ActivityTypeViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Введите название")]
    [MaxLength(200)]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Цвет")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Введите цвет в формате #RRGGBB")]
    public string Color { get; set; } = "#007bff";

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
}
