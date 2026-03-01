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

namespace pomodero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private DispatcherTimer _timer;
    private TimeSpan _remainingTime;
    private TimerMode _currentMode = TimerMode.Work;

    [ObservableProperty]
    private int _workDuration = 25;

    [ObservableProperty]
    private int _shortBreakDuration = 5;

    [ObservableProperty]
    private int _longBreakDuration = 15;

    [ObservableProperty]
    private int _targetSessions = 8;

    [ObservableProperty]
    private int _sessionsCompletedToday = 0;

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
    private bool _isDarkMode = true;

    public MainWindowViewModel()
    {
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

        if (_remainingTime.TotalSeconds <= 0)
        {
            HandleSessionEnd();
        }
    }

    private void HandleSessionEnd()
    {
        _timer.Stop();
        IsRunning = false;
        StartStopButtonText = "Start";
        PlayAlertSound();

        if (_currentMode == TimerMode.Work)
        {
            SessionsCompletedToday++;
            SessionsInCurrentCycle++;

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
    }

    partial void OnWorkDurationChanged(int value) => ResetTimerIfStopped();
    partial void OnShortBreakDurationChanged(int value) => ResetTimerIfStopped();
    partial void OnLongBreakDurationChanged(int value) => ResetTimerIfStopped();

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

public class SessionDotsConverter : IValueConverter
{
    public static readonly SessionDotsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int completed)
        {
            var brushes = new List<IBrush>();
            for (int i = 0; i < 4; i++)
            {
                brushes.Add(i < completed ? Brush.Parse("#FF5F5F") : Brush.Parse("#80808080"));
            }
            return brushes;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public enum TimerMode
{
    Work,
    ShortBreak,
    LongBreak
}
