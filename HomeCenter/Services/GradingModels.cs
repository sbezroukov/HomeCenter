namespace HomeCenter.Services;

public class GradingItem
{
    public string Question { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public string? CorrectAnswer { get; set; }
}
