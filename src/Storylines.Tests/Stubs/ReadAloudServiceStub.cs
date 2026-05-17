
namespace Storylines.Tests.Stubs;

internal sealed class ReadAloudServiceStub : IReadAloudService
{
    public ReadAloudState State { get; private set; } = ReadAloudState.Idle;
    public double Progress { get; private set; }
    public int CurrentParagraphIndex { get; private set; }
    public int TotalParagraphs { get; private set; }

    public event Action<ReadAloudState> StateChanged = delegate { };
    public event Action<double> ProgressChanged = delegate { };
    public event Action Completed = delegate { };

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        TotalParagraphs = string.IsNullOrWhiteSpace(text) ? 0 : 1;
        CurrentParagraphIndex = TotalParagraphs == 0 ? 0 : 1;
        EmitStateChange(ReadAloudState.Playing);
        return Task.CompletedTask;
    }

    public Task SpeakParagraphsAsync(IReadOnlyList<string> paragraphs, int startIndex = 0, CancellationToken cancellationToken = default)
    {
        TotalParagraphs = paragraphs?.Count ?? 0;
        CurrentParagraphIndex = TotalParagraphs == 0 ? 0 : Math.Clamp(startIndex + 1, 1, TotalParagraphs);
        EmitStateChange(TotalParagraphs == 0 ? ReadAloudState.Idle : ReadAloudState.Playing);
        return Task.CompletedTask;
    }

    public Task SpeakSampleAsync(CancellationToken cancellationToken = default)
        => SpeakAsync("sample", cancellationToken);

    public void Pause() => EmitStateChange(ReadAloudState.Paused);

    public void Resume() => EmitStateChange(ReadAloudState.Playing);

    public void Stop()
    {
        CurrentParagraphIndex = 0;
        TotalParagraphs = 0;
        EmitStateChange(ReadAloudState.Idle);
    }

    public Task NextParagraphAsync()
    {
        if (TotalParagraphs > 0)
            CurrentParagraphIndex = Math.Min(CurrentParagraphIndex + 1, TotalParagraphs);

        return Task.CompletedTask;
    }

    public Task PreviousParagraphAsync()
    {
        if (TotalParagraphs > 0)
            CurrentParagraphIndex = Math.Max(CurrentParagraphIndex - 1, 1);

        return Task.CompletedTask;
    }

    public void EmitStateChange(ReadAloudState state)
    {
        State = state;
        StateChanged(state);
        if (state == ReadAloudState.Idle)
            Completed();
    }

    public void EmitProgress(double value)
    {
        Progress = value;
        ProgressChanged(value);
    }
}