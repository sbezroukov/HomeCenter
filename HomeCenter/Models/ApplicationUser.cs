using System;
using System.Collections.Generic;

namespace HomeCenter.Models;

public class ApplicationUser
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    // По заданию — без шифрования, храним как есть
    public string Password { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Telegram Chat ID для отправки уведомлений
    /// </summary>
    public long? TelegramChatId { get; set; }

    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}

