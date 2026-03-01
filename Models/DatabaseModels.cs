using System;

namespace pomodero.Models;

public class DailyRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int CompletedSessions { get; set; }
}

public class UserSettings
{
    public int Id { get; set; }
    public int WorkDuration { get; set; }
    public int ShortBreakDuration { get; set; }
    public int LongBreakDuration { get; set; }
    public int TargetSessions { get; set; }
    public bool IsDarkMode { get; set; }
}
