namespace Storylines.Services;

/// <summary>
/// Bridges <see cref="IUndoRedoService"/> to the existing static <see cref="TimeTravelChapter"/>
/// and <see cref="TimeTravelCharacter"/> helpers and the dirty flag on
/// <see cref="TimeTravelSystem"/>. Callers should migrate to the interface over time; the
/// statics can remain as internal implementation detail until each call site is updated.
/// </summary>
internal sealed class UndoRedoService : IUndoRedoService, IDisposable
{
    private bool _canUndoChapters;
    private bool _canRedoChapters;
    private bool _canUndoCharacters;
    private bool _canRedoCharacters;
    private bool _isDirty;

    public UndoRedoService(EventAggregator events)
    {
        events.Subscribe<UndoRedoStateChangedEvent>(OnStateChanged);
    }

    public bool IsDirty => _isDirty;

    public event Action<string> StateChanged;

    public bool CanUndo(string context) => context switch
    {
        "chapters" => _canUndoChapters,
        "characters" => _canUndoCharacters,
        _ => false
    };

    public bool CanRedo(string context) => context switch
    {
        "chapters" => _canRedoChapters,
        "characters" => _canRedoCharacters,
        _ => false
    };

    public void Undo(string context)
    {
        switch (context)
        {
            case "chapters":
                if (_canUndoChapters)
                    TimeTravelChapter.Undo();
                break;
            case "characters":
                if (_canUndoCharacters)
                    TimeTravelCharacter.Undo();
                break;
        }
    }

    public void Redo(string context)
    {
        switch (context)
        {
            case "chapters":
                if (_canRedoChapters)
                    TimeTravelChapter.Redo();
                break;
            case "characters":
                if (_canRedoCharacters)
                    TimeTravelCharacter.Redo();
                break;
        }
    }

    public void MarkClean() => _isDirty = false;

    public void MarkDirty() => _isDirty = true;

    private void OnStateChanged(UndoRedoStateChangedEvent e)
    {
        switch (e.Context)
        {
            case "chapters":
                _canUndoChapters = e.CanUndo;
                _canRedoChapters = e.CanRedo;
                break;
            case "characters":
                _canUndoCharacters = e.CanUndo;
                _canRedoCharacters = e.CanRedo;
                break;
        }
        StateChanged?.Invoke(e.Context);
    }

    public void Dispose()
    {
        var contextId = App.TryGetService<WindowContext>()?.Id ?? Guid.Empty;
        if (contextId != Guid.Empty)
        {
            TimeTravelChapter.Cleanup(contextId);
            TimeTravelCharacter.Cleanup(contextId);
        }
    }
}
