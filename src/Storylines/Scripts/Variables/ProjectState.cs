using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Storylines.Scripts.Variables
{
    public class ProjectState
    {
        public ObservableCollection<Chapter> Chapters { get; } = new ObservableCollection<Chapter>();
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

        #region Chapter Operations

        public void AddChapter(string name)
        {
            var ch = AddExistingChapter(name, Guid.NewGuid().ToString(), string.Empty);
            TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Added, ch, 0);
        }

        public void AddChapterFromCreator(int i, string txt)
        {
            string chapterName = SettingsValues.chapterName;
            if (chapterName.Contains("{number}"))
                chapterName = chapterName.Replace("{number}", i.ToString());

            AddChapter($"{chapterName}: {txt}");
        }

        public Chapter AddExistingChapter(string name, string token, string text, string notes = "", string synopsis = null, int? wordCountGoal = null)
        {
            var ch = new Chapter() { name = name, token = token, text = text, notes = notes ?? string.Empty, synopsis = synopsis, wordCountGoal = wordCountGoal };
            Chapters.Add(ch);
            return ch;
        }

        public Chapter InsertExistingChapter(string name, string token, string text, int position, string notes = "")
        {
            var ch = new Chapter() { name = name, token = token, text = text, notes = notes ?? string.Empty };
            Chapters.Insert(position, ch);
            return ch;
        }

        public void RenameChapter(string token, string newName)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].token == token)
                {
                    TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Name, Chapters[i], 0);
                    Chapters[i].name = newName;
                }
            }
        }

        public void RemoveChapter(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].token == token)
                {
                    TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Removed, Chapters[i], Chapters.IndexOf(Chapters[i]));
                    Chapters.RemoveAt(i);
                }
            }
        }

        public Chapter CopyChapter(string token)
        {
            return (Chapter)FindChapter(token).MemberwiseClone();
        }

        public Chapter FindChapter(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].token == token)
                    return Chapters[i];
            }
            return null;
        }

        public int FindChapterID(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].token == token)
                    return i;
            }
            return 0;
        }

        public void ReorderChapter(string token, int newPosition, int lastPosition)
        {
            Chapter chapter = FindChapter(token);
            TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Reordered, chapter, lastPosition);

            _ = Chapters.Remove(chapter);
            Chapters.Insert(newPosition, chapter);
        }

        #endregion

        #region Character Operations

        public async Task AddExistingCharacterAsync(string name, string token, string description, CharacterPicture picture, string role = null, string age = null)
        {
            Character ch = new Character()
            {
                name = name,
                token = token,
                description = description,
                role = role,
                age = age,
            };

            if (picture != null)
                if (picture.fileName != null && picture.fileName.Length > 0)
                    ch.picture = new CharacterPicture() { fileName = picture.fileName, image = await Character.LoadProfilePictureAsync(picture) };
                else
                    ch.picture = new CharacterPicture();

            Characters.Add(ch);
        }

        public Character CreateNewCharacter(string name, string description)
        {
            Character ch = new Character() { name = name, token = Guid.NewGuid().ToString(), description = description, picture = new CharacterPicture() };
            Characters.Add(ch);
            TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Added, ch);
            return ch;
        }

        public void RemoveCharacter(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].token == token)
                {
                    TimeTravelCharacter.SomethingChanged(TimeTravelCharacter.Changed.Removed, Characters[i]);
                    _ = Characters.Remove(Characters[i]);
                }
            }
        }

        public Character FindCharacter(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].token == token)
                    return Characters[i];
            }
            return null;
        }

        public int FindCharacterID(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].token == token)
                    return i;
            }
            return 0;
        }

        public Character CopyCharacter(string token)
        {
            return (Character)FindCharacter(token).MemberwiseClone();
        }

        public void SortCharacters()
        {
            Characters = new ObservableCollection<Character>(Characters.OrderBy(o => o.name).ToList());
        }

        #endregion

        public void Clear()
        {
            Chapters.Clear();
            Characters.Clear();
        }
    }
}
