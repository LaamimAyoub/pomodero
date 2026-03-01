using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace pomodero.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private DispatcherTimer _timer;
    private TimeSpan _remainingTime;
    private int _workSessionsCompleted = 0;
    private TimerMode _currentMode = TimerMode.Work;

    [ObservableProperty]
    private string _timeString = "25:00";

    [ObservableProperty]
    private string _statusText = "Work Session";

    [ObservableProperty]
    private string _startStopButtonText = "Start";

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _statusColor = "#FF5F5F"; // Red for work

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
            _workSessionsCompleted++;
            if (_workSessionsCompleted % 4 == 0)
            {
                _currentMode = TimerMode.LongBreak;
                StatusText = "Long Break";
                StatusColor = "#5F5FFF"; // Blue
            }
            else
            {
                _currentMode = TimerMode.ShortBreak;
                StatusText = "Short Break";
                StatusColor = "#5FFF5F"; // Green
            }
        }
        else
        {
            _currentMode = TimerMode.Work;
            StatusText = "Work Session";
            StatusColor = "#FF5F5F"; // Red
        }

        ResetTimer();
        // Automatic transition: Start the next timer automatically
        StartStop();
    }

    private void ResetTimer()
    {
        _remainingTime = _currentMode switch
        {
            TimerMode.Work => TimeSpan.FromMinutes(25),
            TimerMode.ShortBreak => TimeSpan.FromMinutes(5),
            TimerMode.LongBreak => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(25)
        };
        UpdateTimeString();
    }

    private void UpdateTimeString()
    {
        TimeString = $"{_remainingTime.Minutes:D2}:{_remainingTime.Seconds:D2}";
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
        _workSessionsCompleted = 0;
        StatusText = "Work Session";
        StatusColor = "#FF5F5F"; // Red
        ResetTimer();
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
                // Simple beep for Windows
                Console.Beep();
            }
        }
        catch
        {
            // Ignore sound errors
        }
    }
}

public enum TimerMode
{
    Work,
    ShortBreak,
    LongBreak
}
