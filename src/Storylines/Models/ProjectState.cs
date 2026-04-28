using Storylines.Helpers;
using Storylines.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Storylines.Models
{
    public class ProjectState
    {
        public ObservableCollection<Chapter> Chapters { get; } = new ObservableCollection<Chapter>();
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();
        public List<PinboardConnectionData> PinboardConnections { get; set; } = new List<PinboardConnectionData>();
        public List<string> PlotThreads { get; set; } = new List<string>();
        public List<BranchingDialogueGraphData> BranchingDialogues { get; set; } = new List<BranchingDialogueGraphData>();

        #region Chapter Operations

        public void AddChapter(string name)
        {
            var ch = AddExistingChapter(name, Guid.NewGuid().ToString(), string.Empty);
            TimeTravelChapter.RecordAdded(ch, Chapters.Count - 1);
        }

        public void AddChapterFromCreator(int i, string txt)
        {
            string chapterName = SettingsValues.chapterName;
            if (chapterName.Contains("{number}"))
                chapterName = chapterName.Replace("{number}", i.ToString());

            AddChapter($"{chapterName}: {txt}");
        }

        public Chapter AddExistingChapter(string name, string token, string text, string notes = "", string synopsis = null, int? wordCountGoal = null, List<string> tags = null, double pinboardX = 0, double pinboardY = 0, ChapterStatus status = ChapterStatus.Draft, string location = null, List<string> plotThreads = null)
        {
            var ch = new Chapter() { Name = name, Text = text, Notes = notes ?? string.Empty, Synopsis = synopsis, WordCountGoal = wordCountGoal, Tags = tags ?? new List<string>(), PinboardX = pinboardX, PinboardY = pinboardY, Status = status, Location = location, PlotThreads = plotThreads ?? new List<string>() };
            ch.SetToken(token);
            Chapters.Add(ch);
            return ch;
        }

        public Chapter InsertExistingChapter(string name, string token, string text, int position, string notes = "", List<string> tags = null)
        {
            var ch = new Chapter() { Name = name, Text = text, Notes = notes ?? string.Empty, Tags = tags ?? new List<string>() };
            ch.SetToken(token);
            Chapters.Insert(position, ch);
            return ch;
        }

        public void RenameChapter(string token, string newName)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Token == token)
                {
                    TimeTravelChapter.RecordRename(Chapters[i], newName);
                    Chapters[i].Name = newName;
                }
            }
        }

        public void RemoveChapter(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Token == token)
                {
                    TimeTravelChapter.RecordRemoved(Chapters[i], i);

                    // Clean up pinboard connections referencing this chapter
                    int removedIndex = i;
                    PinboardConnections.RemoveAll(c => c.FromIndex == removedIndex || c.ToIndex == removedIndex);
                    foreach (var conn in PinboardConnections)
                    {
                        if (conn.FromIndex > removedIndex) conn.FromIndex--;
                        if (conn.ToIndex > removedIndex) conn.ToIndex--;
                    }

                    BranchingDialogues.RemoveAll(g => g != null && g.ChapterId == token);

                    Chapters.RemoveAt(i);
                    break;
                }
            }
        }

        public Chapter CopyChapter(string token)
        {
            var original = FindChapter(token);
            if (original == null)
                return null;

            var copy = new Chapter()
            {
                Name = original.Name,
                Text = original.Text,
                Notes = original.Notes,
                Synopsis = original.Synopsis,
                WordCountGoal = original.WordCountGoal,
                Tags = original.Tags != null ? new List<string>(original.Tags) : new List<string>(),
                Status = original.Status,
                Location = original.Location,
                PlotThreads = original.PlotThreads != null ? new List<string>(original.PlotThreads) : new List<string>()
            };
            copy.SetToken(original.Token);
            return copy;
        }

        public Chapter FindChapter(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Token == token)
                    return Chapters[i];
            }
            return null;
        }

        public int FindChapterID(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Token == token)
                    return i;
            }
            return 0;
        }

        public void ReorderChapter(string token, int newPosition, int lastPosition)
        {
            Chapter chapter = FindChapter(token);
            TimeTravelChapter.RecordReorder(token, lastPosition, newPosition);

            _ = Chapters.Remove(chapter);
            Chapters.Insert(newPosition, chapter);
        }

        #endregion

        #region Branching Dialogue Operations

        public BranchingDialogueGraphData FindBranchingDialogueByChapter(string chapterToken)
        {
            return BranchingDialogues.FirstOrDefault(g => g?.ChapterId == chapterToken);
        }

        public BranchingDialogueGraphData GetOrCreateBranchingDialogueForChapter(string chapterToken)
        {
            var graph = FindBranchingDialogueByChapter(chapterToken);
            if (graph != null)
            {
                graph.EnsureValid();
                return graph;
            }

            var created = new BranchingDialogueGraphData
            {
                Id = Guid.NewGuid().ToString(),
                ChapterId = chapterToken,
                Nodes = new List<BranchingDialogueNodeData>()
            };
            created.EnsureValid();
            BranchingDialogues.Add(created);
            return created;
        }

        public void SetBranchingDialogues(List<BranchingDialogueGraphData> graphs)
        {
            BranchingDialogues = graphs ?? new List<BranchingDialogueGraphData>();
            foreach (var graph in BranchingDialogues)
                graph?.EnsureValid();
        }

        #endregion

        #region Character Operations

        public async Task AddExistingCharacterAsync(string name, string token, string description, CharacterPicture picture, string role = null, string age = null, string appearance = null, List<string> traits = null)
        {
            Character ch = new Character()
            {
                Name = name,
                Description = description,
                Role = role,
                Age = age,
                Appearance = appearance,
                Traits = traits?.ToList() ?? new List<string>(),
            };

            ch.SetToken(token);

            if (picture != null)
                if (picture.FileName != null && picture.FileName.Length > 0)
                    ch.Picture = new CharacterPicture() { FileName = picture.FileName, Image = await Character.LoadProfilePictureAsync(picture) };
                else
                    ch.Picture = new CharacterPicture();

            Characters.Add(ch);
        }

        public Character CreateNewCharacter(string name, string description)
        {
            Character ch = new Character() { Name = name, Description = description, Picture = new CharacterPicture(), Traits = new List<string>() };
            ch.SetToken(Guid.NewGuid().ToString());
            Characters.Add(ch);
            TimeTravelCharacter.RecordAdded(ch);
            return ch;
        }

        public void RemoveCharacter(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Token == token)
                {
                    TimeTravelCharacter.RecordRemoved(Characters[i]);
                    _ = Characters.Remove(Characters[i]);
                    break;
                }
            }
        }

        public Character FindCharacter(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Token == token)
                    return Characters[i];
            }
            return null;
        }

        public int FindCharacterID(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Token == token)
                    return i;
            }
            return 0;
        }

        public Character CopyCharacter(string token)
        {
            var character = FindCharacter(token);
            if (character == null)
                return null;

            return new Character()
            {
                Name = character.Name,
                Description = character.Description,
                Role = character.Role,
                Age = character.Age,
                Appearance = character.Appearance,
                Picture = character.Picture == null
                    ? null
                    : new CharacterPicture()
                    {
                        FileName = character.Picture.FileName,
                        Image = character.Picture.Image,
                        LocalFilePath = character.Picture.LocalFilePath,
                    },
                Traits = character.Traits?.ToList() ?? new List<string>(),
            }.WithToken(character.Token);
        }

        public void SortCharacters()
        {
            var sorted = Characters.OrderBy(o => o.Name).ToList();
            Characters.Clear();
            foreach (var character in sorted)
                Characters.Add(character);
        }

        #endregion

        public void Clear()
        {
            Chapters.Clear();
            Characters.Clear();
            PinboardConnections.Clear();
            PlotThreads.Clear();
            BranchingDialogues.Clear();
        }

    }

    internal static class CharacterExtensions
    {
        public static Character WithToken(this Character character, string token)
        {
            character.SetToken(token);
            return character;
        }
    }
}
