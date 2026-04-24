using Storylines.DataStructures;
using Storylines.Services;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Helpers
{
    class TimeTravelSystem
    {
        private static EventAggregator Events => App.GetService<EventAggregator>();

        public static bool unSavedProgress = false;

        public static bool timeTravelling;

        public static void SomethingChanged()
        {
            unSavedProgress = true;
            Events.Publish(new TitleBarUpdateEvent());
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

        private static ChaptersListViewModel ChaptersListViewModel => App.GetService<ChaptersListViewModel>();
        private static EventAggregator Events => App.GetService<EventAggregator>();
        private static ProjectState State => App.GetService<ProjectState>();
        private static ITextEditorService TextEditor => App.GetService<ITextEditorService>();

        public enum Changed { Added, Name, Text, Reordered, Removed };

        public static void SomethingChanged(Changed whatChanged, Chapter chapter, int lastPosition)
        {
            if (!TimeTravelSystem.timeTravelling && !ChaptersListViewModel.SwitchedChapters)
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
                        tt = new TimeTravelChapter{ chapter = new Chapter { Name = chapter.Name } };
                        break;
                    case Changed.Text:
                        tt = new TimeTravelChapter { chapter = new Chapter { Text = chapter.Text } };
                        break;
                }
                tt.chapter.SetToken(chapter.Token);

                tt.changed = whatChanged;
                tt.id = TextEditor.SelectedChapterIndex;
                tt.lastPosition = lastPosition;

                if(whatChanged == Changed.Text)
                    TryGroupingUndoQueue();

                undoQueue.Push(tt);
                redoQueue.Clear();
                CheckForUndoOrRedoEmpty();
            }

            TimeTravelSystem.timeTravelling = false;
            ChaptersListViewModel.SwitchedChapters = false;
        }

        public static void Undo()
        {
            if (undoQueue.Count > 0)
            {
                var timeTravel = undoQueue.Pop();
                if(timeTravel.changed != Changed.Removed)
                    redoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = State.CopyChapter(timeTravel.chapter.Token), id = timeTravel.lastPosition, lastPosition = State.FindChapterID(timeTravel.chapter.Token) });
                else
                    redoQueue.Push(timeTravel);
                ChapterThings(timeTravel, false);
            }
        }

        public static void Redo()
        {
            if (redoQueue.Count > 0)
            {
                var timeTravel = redoQueue.Pop();
                if (timeTravel.changed != Changed.Added)
                    undoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = State.CopyChapter(timeTravel.chapter.Token), id = State.FindChapterID(timeTravel.chapter.Token), lastPosition = timeTravel.lastPosition });
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
                        State.RemoveChapter(timeTravel.chapter.Token);
                    else
                        _ = State.InsertExistingChapter(timeTravel.chapter.Name, timeTravel.chapter.Token, timeTravel.chapter.Text, timeTravel.lastPosition);
                    break;
                    case Changed.Name:
                        State.RenameChapter(timeTravel.chapter.Token, timeTravel.chapter.Name);
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
                        State.ReorderChapter(timeTravel.chapter.Token, timeTravel.lastPosition, 0);
                    else
                        State.ReorderChapter(timeTravel.chapter.Token, timeTravel.id, 0);
                    break;

                case Changed.Removed:
                    if (!isRedo)
                        State.InsertExistingChapter(timeTravel.chapter.Name, timeTravel.chapter.Token, timeTravel.chapter.Text, timeTravel.lastPosition);
                    else
                        State.RemoveChapter(timeTravel.chapter.Token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            CheckForUndoOrRedoEmpty();
        }

        public static void ClearUndoAndRedo()
        {
            undoQueue.Clear();
            redoQueue.Clear();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = undoQueue.Count > 0,
                CanRedo = redoQueue.Count > 0,
                Context = "chapters"
            });
        }

        private static void TryGroupingUndoQueue()
        {
            var undoQueueArray = undoQueue.items.ToArray();
            for (int i = 1; i < undoQueueArray.Length; i++)
            {
                if (undoQueueArray[i - 1] != null && undoQueueArray[i - 1].changed == undoQueueArray[i].changed && undoQueueArray[i - 1].chapter.Text != null && undoQueueArray[i].chapter.Text != null)
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
        public Changed changed;

        private static readonly Stack<TimeTravelCharacter> undoQueue = new Stack<TimeTravelCharacter>();
        private static readonly Stack<TimeTravelCharacter> redoQueue = new Stack<TimeTravelCharacter>();

        private static EventAggregator Events => App.GetService<EventAggregator>();
        private static ProjectState State => App.GetService<ProjectState>();

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
                        tt.character = State.CopyCharacter(character.Token);
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
                    redoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = State.CopyCharacter(timeTravel.character.Token) });
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
                    undoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = State.CopyCharacter(timeTravel.character.Token) });
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
                        State.RemoveCharacter(timeTravel.character.Token);
                    else
                        await State.AddExistingCharacterAsync(timeTravel.character.Name, timeTravel.character.Token, timeTravel.character.Description, timeTravel.character.Picture, timeTravel.character.Role, timeTravel.character.Age, timeTravel.character.Appearance, timeTravel.character.Traits);
                    break;
                case Changed.Changed:
                    var chID = State.FindCharacterID(timeTravel.character.Token);
                    State.Characters[chID] = timeTravel.character;
                    Events.Publish(new CharacterSelectedEvent { SelectedIndex = chID, HasSelection = true });
                    break;
                case Changed.Removed:
                    if (!isRedo)
                        await State.AddExistingCharacterAsync(timeTravel.character.Name, timeTravel.character.Token, timeTravel.character.Description, timeTravel.character.Picture, timeTravel.character.Role, timeTravel.character.Age, timeTravel.character.Appearance, timeTravel.character.Traits);
                    else
                        State.RemoveCharacter(timeTravel.character.Token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            Events.Publish(new TitleBarUpdateEvent());
            CheckForUndoOrRedoEmpty();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = undoQueue.Count > 0,
                CanRedo = redoQueue.Count > 0,
                Context = "characters"
            });
        }
    }

}
