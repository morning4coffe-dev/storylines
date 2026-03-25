using Storylines.Scripts.Services;
using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Variables;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Scripts.Functions
{
    class TimeTravelSystem
    {
        public static bool unSavedProgress = false;

        public static bool timeTravelling;

        public static void SomethingChanged()
        {
            unSavedProgress = true;
            ServiceLocator.Events.Publish(new TitleBarUpdateEvent());
        }
    }

    public class TimeTravelChapter
    {
        public int id;
        public Chapter chapter;
        public Changed changed;

        public int lastPosition;

        private static readonly PartialStack<TimeTravelChapter> undoQueue = new PartialStack<TimeTravelChapter>();
        private static readonly PartialStack<TimeTravelChapter> redoQueue = new PartialStack<TimeTravelChapter>();

        private static ProjectState State => ServiceLocator.ProjectState;
        private static ITextEditorService TextEditor => ServiceLocator.TextEditor;

        public enum Changed { Added, Name, Text, Reordered, Removed };

        public static void SomethingChanged(Changed whatChanged, Chapter chapter, int lastPosition)
        {
            if (!TimeTravelSystem.timeTravelling && !ServiceLocator.ChaptersListViewModel.SwitchedChapters)
            {
                TimeTravelSystem.SomethingChanged();
                TimeTravelChapter tt = new TimeTravelChapter();

                switch (whatChanged)
                {
                    case Changed.Added:
                        case Changed.Reordered:
                            case Changed.Removed:
                                tt = new TimeTravelChapter() { chapter = chapter };
                                break;
                    case Changed.Name:
                        tt = new TimeTravelChapter{ chapter = new Chapter { name = chapter.name } };
                        break;
                    case Changed.Text:
                        tt = new TimeTravelChapter { chapter = new Chapter { text = chapter.text } };
                        break;
                }
                tt.chapter.SetToken(chapter.token);

                tt.changed = whatChanged;
                tt.id = TextEditor.SelectedChapterIndex;
                tt.lastPosition = lastPosition;

                if(whatChanged == Changed.Text)
                    TryGroupingUndoQueue();

                undoQueue.Push(tt);
                redoQueue.items.Clear();
                CheckForUndoOrRedoEmpty();
            }

            TimeTravelSystem.timeTravelling = false;
            ServiceLocator.ChaptersListViewModel.SwitchedChapters = false;
        }

        public static void Undo()
        {
            if (undoQueue.items.Count > 0)
            {
                var timeTravel = undoQueue.Pop();
                if(timeTravel.changed != Changed.Removed)
                    redoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = State.CopyChapter(timeTravel.chapter.token), id = timeTravel.lastPosition, lastPosition = State.FindChapterID(timeTravel.chapter.token) });
                else
                    redoQueue.Push(timeTravel);
                ChapterThings(timeTravel, false);
            }
        }

        public static void Redo()
        {
            if (redoQueue.items.Count > 0)
            {
                var timeTravel = redoQueue.Pop();
                if (timeTravel.changed != Changed.Added)
                    undoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = State.CopyChapter(timeTravel.chapter.token), id = State.FindChapterID(timeTravel.chapter.token), lastPosition = timeTravel.lastPosition });
                else
                    undoQueue.Push(timeTravel);
                ChapterThings(timeTravel, true);
            }
        }

        private static void ChapterThings(TimeTravelChapter timeTravel, bool isRedo)
        {
            TimeTravelSystem.timeTravelling = true;

            var ttId = timeTravel.id;

            switch (timeTravel.changed)
            {
                case Changed.Added:
                    if (!isRedo)
                        State.RemoveChapter(timeTravel.chapter.token);
                    else
                        _ = State.InsertExistingChapter(timeTravel.chapter.name, timeTravel.chapter.token, timeTravel.chapter.text, timeTravel.lastPosition);
                    break;
                    case Changed.Name:
                        State.RenameChapter(timeTravel.chapter.token, timeTravel.chapter.name);
                    break;

                    case Changed.Text:
                        TextEditor.SelectedChapterIndex = ttId;
                    if (!isRedo)
                        TextEditor.Undo();
                    else
                        TextEditor.Redo();
                    break;

                case Changed.Reordered:
                    if (!isRedo)
                        State.ReorderChapter(timeTravel.chapter.token, timeTravel.lastPosition, 0);
                    else
                        State.ReorderChapter(timeTravel.chapter.token, timeTravel.id, 0);
                    break;

                case Changed.Removed:
                    if (!isRedo)
                        State.InsertExistingChapter(timeTravel.chapter.name, timeTravel.chapter.token, timeTravel.chapter.text, timeTravel.lastPosition);
                    else
                        State.RemoveChapter(timeTravel.chapter.token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            CheckForUndoOrRedoEmpty();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            ServiceLocator.Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = undoQueue.items.Count > 0,
                CanRedo = redoQueue.items.Count > 0,
                Context = "chapters"
            });
        }

        private static void TryGroupingUndoQueue()
        {
            var undoQueueArray = undoQueue.items.ToArray();
            for (int i = 1; i < undoQueueArray.Length; i++)
            {
                if (undoQueueArray[i - 1] != null && undoQueueArray[i - 1].changed == undoQueueArray[i].changed && undoQueueArray[i - 1].chapter.text != null && undoQueueArray[i].chapter.text != null)
                {
                    undoQueue.items[i - 1] = undoQueueArray[i];
                    undoQueue.items.RemoveAt(i);
                }
            }
        }
    }

    public class TimeTravelCharacter
    {
        public Character character;
        public Character characterUndo;
        public Changed changed;

        private static readonly Stack<TimeTravelCharacter> undoQueue = new Stack<TimeTravelCharacter>();
        private static readonly Stack<TimeTravelCharacter> redoQueue = new Stack<TimeTravelCharacter>();

        private static ProjectState State => ServiceLocator.ProjectState;

        public static void ClearUndoAndRedo()
        {
            undoQueue.Clear();
            redoQueue.Clear();
        }

        public enum Changed { Added, Changed, Removed };

        public static void SomethingChanged(Changed whatChanged, Character character)
        {
            if (!TimeTravelSystem.timeTravelling)
            {
                TimeTravelSystem.SomethingChanged();

                TimeTravelCharacter tt = new TimeTravelCharacter();

                switch (whatChanged)
                {
                    case Changed.Added:
                    case Changed.Removed:
                        tt = new TimeTravelCharacter() { character = character };
                        break;
                    case Changed.Changed:
                        tt = new TimeTravelCharacter();
                        tt.character = State.CopyCharacter(character.token);
                        break;
                }

                tt.changed = whatChanged;

                undoQueue.Push(tt);
                redoQueue.Clear();
                CheckForUndoOrRedoEmpty();
            }

            TimeTravelSystem.timeTravelling = false;
        }

        public static void Undo()
        {
            if (undoQueue.Count > 0)
            {
                var timeTravel = undoQueue.Pop();
                if (timeTravel.changed == Changed.Changed)
                    redoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = State.CopyCharacter(timeTravel.character.token) });
                else
                    redoQueue.Push(timeTravel);

                UndoRedoShared(timeTravel, false);
                CheckForUndoOrRedoEmpty();
            }
        }

        public static void Redo()
        {
            if (redoQueue.Count > 0)
            {
                var timeTravel = redoQueue.Pop();
                if (timeTravel.changed == Changed.Changed)
                    undoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = State.CopyCharacter(timeTravel.character.token) });
                else
                    undoQueue.Push(timeTravel);

                UndoRedoShared(timeTravel, true);
                CheckForUndoOrRedoEmpty();
            }
        }

        private static async void UndoRedoShared(TimeTravelCharacter timeTravel, bool isRedo)
        {
            TimeTravelSystem.timeTravelling = true;

            switch (timeTravel.changed)
            {
                case Changed.Added:
                    if (!isRedo)
                        State.RemoveCharacter(timeTravel.character.token);
                    else
                        await State.AddExistingCharacterAsync(timeTravel.character.name, timeTravel.character.token, timeTravel.character.description, timeTravel.character.picture, timeTravel.character.role, timeTravel.character.age, timeTravel.character.appearance, timeTravel.character.traits);
                    break;
                case Changed.Changed:
                    var chID = State.FindCharacterID(timeTravel.character.token);
                    State.Characters[chID] = timeTravel.character;
                    ServiceLocator.Events.Publish(new CharacterSelectedEvent { SelectedIndex = chID, HasSelection = true });
                    break;
                case Changed.Removed:
                    if (!isRedo)
                        await State.AddExistingCharacterAsync(timeTravel.character.name, timeTravel.character.token, timeTravel.character.description, timeTravel.character.picture, timeTravel.character.role, timeTravel.character.age, timeTravel.character.appearance, timeTravel.character.traits);
                    else
                        State.RemoveCharacter(timeTravel.character.token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            ServiceLocator.Events.Publish(new TitleBarUpdateEvent());
            CheckForUndoOrRedoEmpty();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            ServiceLocator.Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = undoQueue.Count > 0,
                CanRedo = redoQueue.Count > 0,
                Context = "characters"
            });
        }
    }

    public class PartialStack<T>
    {
        public List<T> items = new List<T>();

        public void Push(T item)
        {
            items.Add(item);
        }

        public T Pop()
        {
            if (items.Count > 0)
            {
                T temp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return temp;
            }
            else
                return default;
        }
    }
}
