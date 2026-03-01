using System;
using System.Linq;
using LiteDB;
using pomodero.Models;

namespace pomodero.Services;

public class DatabaseService : IDisposable
{
    private readonly LiteDatabase _db;
    private const string DB_NAME = "pomodero.db";

    public DatabaseService()
    {
        _db = new LiteDatabase(DB_NAME);
    }

    public UserSettings GetSettings()
    {
        var collection = _db.GetCollection<UserSettings>("settings");
        var settings = collection.FindAll().FirstOrDefault();
        if (settings == null)
        {
            settings = new UserSettings
            {
                WorkDuration = 25,
                ShortBreakDuration = 5,
                LongBreakDuration = 15,
                TargetSessions = 8,
                IsDarkMode = true
            };
            collection.Insert(settings);
        }
        return settings;
    }

    public void SaveSettings(UserSettings settings)
    {
        var collection = _db.GetCollection<UserSettings>("settings");
        collection.Update(settings);
    }

    public DailyRecord GetTodayRecord()
    {
        var collection = _db.GetCollection<DailyRecord>("records");
        var today = DateTime.Today;
        var record = collection.Find(x => x.Date == today).FirstOrDefault();
        if (record == null)
        {
            record = new DailyRecord { Date = today, CompletedSessions = 0 };
            collection.Insert(record);
        }
        return record;
    }

    public void SaveTodayRecord(DailyRecord record)
    {
        var collection = _db.GetCollection<DailyRecord>("records");
        collection.Update(record);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
