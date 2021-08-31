using Newtonsoft.Json.Linq;
using Storylines.Pages;
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
            AppView.current.UpdateTitleBar();
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

        public enum Changed { Added, Name, Text, Reordered, Removed };

        public static void SomethingChanged(Changed whatChanged, Chapter chapter, int lastPosition)
        {
            if (!TimeTravelSystem.timeTravelling && !MainPage.chapterList.switchedChapters)
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
                tt.id = MainPage.chapterList.listView.Items.IndexOf(chapter);
                tt.lastPosition = lastPosition;

                if(whatChanged == Changed.Text)
                    TryGroupingUndoQueue();

                undoQueue.Push(tt);
                redoQueue.items.Clear();
                CheckForUndoOrRedoEmpty();
            }

            TimeTravelSystem.timeTravelling = false;
            MainPage.chapterList.switchedChapters = false;
        }

        public static void Undo()
        {
            if (undoQueue.items.Count > 0)
            {
                var timeTravel = undoQueue.Pop();
                if(timeTravel.changed != Changed.Removed)
                    redoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = Chapter.Copy(timeTravel.chapter.token), id = timeTravel.lastPosition, lastPosition = Chapter.FindID(timeTravel.chapter.token) });
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
                    undoQueue.Push(new TimeTravelChapter() { changed = timeTravel.changed, chapter = Chapter.Copy(timeTravel.chapter.token), id = Chapter.FindID(timeTravel.chapter.token), lastPosition = timeTravel.lastPosition });
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
                        Chapter.Remove(timeTravel.chapter.token);
                    else
                        _ = Chapter.InsertExisting(timeTravel.chapter.name, timeTravel.chapter.token, timeTravel.chapter.text, timeTravel.lastPosition);
                    break;
                    case Changed.Name:
                        Chapter.Rename(timeTravel.chapter.token, timeTravel.chapter.name);
                    break;

                    case Changed.Text:
                        MainPage.chapterList.listView.SelectedIndex = ttId;
                    if (!isRedo)
                        MainPage.chapterText.textBox.Document.Undo();
                    else
                        MainPage.chapterText.textBox.Document.Redo();
                    break;

                case Changed.Reordered:
                    if (!isRedo)
                        Chapter.Reorder(timeTravel.chapter.token, timeTravel.lastPosition, 0);
                    else
                        Chapter.Reorder(timeTravel.chapter.token, timeTravel.id, 0);
                    break;

                case Changed.Removed:
                    if (!isRedo)
                        Chapter.InsertExisting(timeTravel.chapter.name, timeTravel.chapter.token, timeTravel.chapter.text, timeTravel.lastPosition);
                    else
                        Chapter.Remove(timeTravel.chapter.token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            CheckForUndoOrRedoEmpty();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            MainPage.commandBar.undoButton.IsEnabled = undoQueue.items.Count > 0;

            MainPage.commandBar.redoButton.IsEnabled = redoQueue.items.Count > 0;
        }

        private static void TryGroupingUndoQueue()
        {
            var undoQueueArray = undoQueue.items.ToArray();
            for (int i = 0; i < undoQueueArray.Length; i++)
            {
                try
                {
                    if (undoQueueArray[i - 1] != null && undoQueueArray[i - 1].changed == undoQueueArray[i].changed && undoQueueArray[i - 1].chapter.text != null && undoQueueArray[i].chapter.text != null)
                    {
                        undoQueue.items[i - 1] = undoQueueArray[i];
                        undoQueue.items.RemoveAt(i);
                    }
                }
                catch { }
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
                        tt.character = Character.Copy(character.token);
                        //tt.character = new Character()
                        //{
                        //    name = character.name,
                        //    description = character.description,
                        //    picture = character.picture,
                        //};
                        //tt.character.SetToken(character.token);
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
                    redoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = Character.Copy(timeTravel.character.token) });
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
                    undoQueue.Push(new TimeTravelCharacter() { changed = timeTravel.changed, character = Character.Copy(timeTravel.character.token) });
                else
                    undoQueue.Push(timeTravel);

                UndoRedoShared(timeTravel, true);
                CheckForUndoOrRedoEmpty();
            }
        }

        private static void UndoRedoShared(TimeTravelCharacter timeTravel, bool isRedo)
        {
            TimeTravelSystem.timeTravelling = true;

            switch (timeTravel.changed)
            {
                case Changed.Added:
                    if (!isRedo)
                        Character.Remove(timeTravel.character.token);
                    else
                        Character.AddExisting(timeTravel.character.name, timeTravel.character.token, timeTravel.character.description, timeTravel.character.picture);
                    break;
                case Changed.Changed:
                    var chID = Character.FindID(timeTravel.character.token);
                    Character.characters[chID] = timeTravel.character;
                    CharactersPage.current.listView.SelectedIndex = chID;
                    break;
                case Changed.Removed:
                    if (!isRedo)
                        Character.AddExisting(timeTravel.character.name, timeTravel.character.token, timeTravel.character.description, timeTravel.character.picture);
                    else
                        Character.Remove(timeTravel.character.token);
                    break;
            }
            TimeTravelSystem.timeTravelling = false;
            CharactersPage.current.Sort();
            CheckForUndoOrRedoEmpty();
        }

        private static void CheckForUndoOrRedoEmpty()
        {
            CharactersPage.current.undoButton.IsEnabled = undoQueue.Count > 0;

            CharactersPage.current.redoButton.IsEnabled = redoQueue.Count > 0;
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
