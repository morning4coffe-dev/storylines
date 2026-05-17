using Storylines.Tests.Stubs;
using Xunit;

namespace Storylines.Tests.Services;

public class WritingStatsServiceTests
{
    [Fact]
    public void RecordSnapshot_TracksDailyDelta()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.RecordSnapshot(100);
        stats.RecordSnapshot(150);

        Assert.Equal(50, stats.WordsToday);
    }

    [Fact]
    public void RecordSnapshot_NegativeDelta_ClampsToZero()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.RecordSnapshot(200);
        stats.RecordSnapshot(150);

        Assert.Equal(0, stats.WordsToday);
    }

    [Fact]
    public void StartSession_TracksSessionWordsIndependentlyOfDaily()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.RecordSnapshot(100);
        stats.StartSession(100);
        stats.RecordSnapshot(180);

        Assert.Equal(80, stats.SessionWords);
        Assert.Equal(80, stats.WordsToday);
    }

    [Fact]
    public void EndSession_StopsSessionTracking()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.StartSession(0);
        stats.RecordSnapshot(50);
        Assert.True(stats.IsSessionActive);

        stats.EndSession();

        Assert.False(stats.IsSessionActive);
        Assert.Equal(0, stats.SessionWords);
    }

    [Fact]
    public void SetDailyGoal_PersistsToSettings()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.SetDailyGoal(750);

        Assert.Equal(750, stats.DailyGoal);
        Assert.Equal(750, settings.DailyWordGoal);
    }

    [Fact]
    public void SetDailyGoal_NegativeValue_ClampsToZero()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);

        stats.SetDailyGoal(-100);

        Assert.Equal(0, stats.DailyGoal);
    }

    [Fact]
    public void StatsChanged_FiresOnEverySnapshot()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);
        int eventCount = 0;
        stats.StatsChanged += () => eventCount++;

        stats.RecordSnapshot(10);
        stats.RecordSnapshot(20);
        stats.RecordSnapshot(30);

        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void StreakIncrements_WhenGoalMetWithinDay()
    {
        var settings = new AppSettingsServiceStub();
        var stats = new WritingStatsService(settings);
        stats.SetDailyGoal(50);

        stats.RecordSnapshot(0);
        stats.RecordSnapshot(60);

        Assert.Equal(1, stats.CurrentStreakDays);
    }
}
