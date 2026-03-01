using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using pomodero.Models;
using pomodero.Services;

namespace pomodero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private DispatcherTimer _timer;
    private TimeSpan _remainingTime;
    private TimerMode _currentMode = TimerMode.Work;
    private readonly DatabaseService _db;
    private UserSettings _settings;
    private DailyRecord _todayRecord;

    [ObservableProperty]
    private int _workDuration;

    [ObservableProperty]
    private int _shortBreakDuration;

    [ObservableProperty]
    private int _longBreakDuration;

    [ObservableProperty]
    private int _targetSessions;

    [ObservableProperty]
    private int _sessionsCompletedToday;

    [ObservableProperty]
    private int _sessionsInCurrentCycle = 0;

    [ObservableProperty]
    private string _timeString = "25:00";

    [ObservableProperty]
    private string _statusText = "Work Session";

    [ObservableProperty]
    private string _startStopButtonText = "Start";

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _statusColor = "#FF5F5F";

    [ObservableProperty]
    private bool _isDarkMode;

    private TimeSpan _totalSessionTime;

    [ObservableProperty]
    private double _progressSweepAngle = 0;

    public MainWindowViewModel()
    {
        _db = new DatabaseService();
        _settings = _db.GetSettings();
        _todayRecord = _db.GetTodayRecord();

        // Load settings from DB
        _workDuration = _settings.WorkDuration;
        _shortBreakDuration = _settings.ShortBreakDuration;
        _longBreakDuration = _settings.LongBreakDuration;
        _targetSessions = _settings.TargetSessions;
        _isDarkMode = _settings.IsDarkMode;
        _sessionsCompletedToday = _todayRecord.CompletedSessions;

        // Apply theme on start
        Dispatcher.UIThread.Post(() => {
            var app = Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        });

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        ResetTimer();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
        UpdateTimeString();
        UpdateProgress();

        if (_remainingTime.TotalSeconds <= 0)
        {
            HandleSessionEnd();
        }
    }

    private void UpdateProgress()
    {
        if (_totalSessionTime.TotalSeconds > 0)
        {
            double progress = 1.0 - (_remainingTime.TotalSeconds / _totalSessionTime.TotalSeconds);
            ProgressSweepAngle = progress * 360;
        }
        else
        {
            ProgressSweepAngle = 0;
        }
    }

    private void HandleSessionEnd()
    {
        _timer.Stop();
        IsRunning = false;
        StartStopButtonText = "Start";
        ProgressSweepAngle = 360;
        PlayAlertSound();

        if (_currentMode == TimerMode.Work)
        {
            SessionsCompletedToday++;
            SessionsInCurrentCycle++;

            // Save to DB
            _todayRecord.CompletedSessions = SessionsCompletedToday;
            _db.SaveTodayRecord(_todayRecord);

            if (SessionsInCurrentCycle >= 4)
            {
                _currentMode = TimerMode.LongBreak;
                StatusText = "Long Break";
                StatusColor = "#5F5FFF";
                SessionsInCurrentCycle = 0;
            }
            else
            {
                _currentMode = TimerMode.ShortBreak;
                StatusText = "Short Break";
                StatusColor = "#5FFF5F";
            }
        }
        else
        {
            _currentMode = TimerMode.Work;
            StatusText = "Work Session";
            StatusColor = "#FF5F5F";
        }

        ResetTimer();
        StartStop();
    }

    private void ResetTimer()
    {
        _remainingTime = _currentMode switch
        {
            TimerMode.Work => TimeSpan.FromMinutes(WorkDuration),
            TimerMode.ShortBreak => TimeSpan.FromMinutes(ShortBreakDuration),
            TimerMode.LongBreak => TimeSpan.FromMinutes(LongBreakDuration),
            _ => TimeSpan.FromMinutes(WorkDuration)
        };
        _totalSessionTime = _remainingTime;
        ProgressSweepAngle = 0;
        UpdateTimeString();
    }

    private void UpdateTimeString()
    {
        TimeString = $"{(int)_remainingTime.TotalMinutes:D2}:{_remainingTime.Seconds:D2}";
    }

    [RelayCommand]
    private void StartStop()
    {
        if (IsRunning)
        {
            _timer.Stop();
            IsRunning = false;
            StartStopButtonText = "Start";
        }
        else
        {
            if (_remainingTime.TotalSeconds <= 0)
            {
                ResetTimer();
            }
            _timer.Start();
            IsRunning = true;
            StartStopButtonText = "Pause";
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        IsRunning = false;
        StartStopButtonText = "Start";
        _currentMode = TimerMode.Work;
        SessionsInCurrentCycle = 0;
        StatusText = "Work Session";
        StatusColor = "#FF5F5F";
        ResetTimer();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var app = Application.Current;
        if (app != null)
        {
            app.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
        SaveSettings();
    }

    partial void OnWorkDurationChanged(int value)
    {
        SaveSettings();
        ResetTimerIfStopped();
    }

    partial void OnShortBreakDurationChanged(int value)
    {
        SaveSettings();
        ResetTimerIfStopped();
    }

    partial void OnLongBreakDurationChanged(int value)
    {
        SaveSettings();
        ResetTimerIfStopped();
    }

    partial void OnTargetSessionsChanged(int value)
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settings.WorkDuration = WorkDuration;
        _settings.ShortBreakDuration = ShortBreakDuration;
        _settings.LongBreakDuration = LongBreakDuration;
        _settings.TargetSessions = TargetSessions;
        _settings.IsDarkMode = IsDarkMode;
        _db.SaveSettings(_settings);
    }

    private void ResetTimerIfStopped()
    {
        if (!IsRunning)
        {
            ResetTimer();
        }
    }

    private void PlayAlertSound()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("aplay", "/usr/share/sounds/alsa/Front_Center.wav");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Beep();
            }
        }
        catch { }
    }
}

public class ThemeTextConverter : IValueConverter
{
    public static readonly ThemeTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDark)
        {
            return isDark ? "Dark Mode" : "Light Mode";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SessionGroupsConverter : IMultiValueConverter
{
    public static readonly SessionGroupsConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is int completed && values[1] is int target)
        {
            var groups = new List<List<IBrush>>();
            for (int i = 0; i < target; i += 4)
            {
                var group = new List<IBrush>();
                for (int j = i; j < Math.Min(i + 4, target); j++)
                {
                    group.Add(j < completed ? Brush.Parse("#FF5F5F") : Brush.Parse("#80808080"));
                }
                groups.Add(group);
            }
            return groups;
        }
        return null;
    }
}

public enum TimerMode
{
    Work,
    ShortBreak,
    LongBreak
}
