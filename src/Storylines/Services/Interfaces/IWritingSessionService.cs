namespace Storylines.Services.Interfaces;

public interface IWritingSessionService
{
    WritingSessionData Current { get; }
    void OnSessionStart(int currentProjectWordCount);
    void RecordWords(int currentProjectWordCount);
    void OnDayCompleted();
    int GetCurrentStreak();
    int GetTodayWords();
}
