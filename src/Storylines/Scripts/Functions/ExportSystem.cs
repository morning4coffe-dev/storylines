using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components
{
    public class ExportSystem
    {
        public enum WhatToExport { None, Chapters, Dialogues, Characters };
        public static WhatToExport export;

        public static void Export(StorageFolder folder, string fileName, string selectedExtension, List<int> chapterOrCharacterNumbers, List<Character> dialogueCharacters, bool withChapterName)
        {
            if (folder != null && export != default)
            {
                switch (export)
                {
                    case WhatToExport.Chapters:
                        _ = ExportChapters(folder, fileName, selectedExtension, chapterOrCharacterNumbers, withChapterName);
                        break;
                    case WhatToExport.Dialogues:
                        var dialogueCharacterNames = new List<string>();

                        foreach (var item in dialogueCharacters)
                        {
                            dialogueCharacterNames.Add(item.name);
                        }
                        _ = ExportDialogues(folder, fileName, selectedExtension, chapterOrCharacterNumbers, dialogueCharacterNames);
                        break;
                    case WhatToExport.Characters:
                        _ = ExportCharacters(folder, fileName, selectedExtension, dialogueCharacters);
                        break;
                }
            }
        }

        #region Chapters
        private static async Task ExportChapters(StorageFolder folder, string fileName, string extension, List<int> chapterNumbers, bool withChapterName)
        {                
            string toExport = "";
            RichEditBox box = new RichEditBox();

            try
            {
                var storageFile = await folder.CreateFileAsync($"{fileName}{extension}", CreationCollisionOption.ReplaceExisting);

                if (extension == ".txt")
                {
                    for (int i = 0; i < chapterNumbers.Count; i++)
                    {
                        box.Document.SetText(TextSetOptions.FormatRtf, Chapter.chapters[chapterNumbers[i]].text);
                        box.Document.GetText(TextGetOptions.None, out string txt);

                        toExport += $"{(withChapterName ? $"{Chapter.chapters[chapterNumbers[i]].name}\n" : string.Empty)}{txt}\n";
                    }
                }
                else
                if (extension == ".rtf")
                {
                    _ = ExportChaptersToRtf(storageFile, chapterNumbers, withChapterName);
                    return;
                }

                await FileIO.WriteTextAsync(storageFile, toExport);
                }
            catch (Exception)
            {
                //notification system.notify
            }
        }


        private static async Task ExportChaptersToRtf(StorageFile file, List<int> chapterNumbers, bool withChapterName)
        {
            RichEditBox box = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };
            string[] txts = new string[chapterNumbers.Count];

            for (int i = 0; i < chapterNumbers.Count; i++)
            {
                if (withChapterName)
                {
                    RichEditBox box2 = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };

                    string rtf1 = @"{\rtf1{\fonttbl{\f0 Segoe UI;}{\f1 Calibri;}{\f2 Verdana;}}{\colortbl;\red255\green255\blue255;\red0\green0\blue0;}\f0\b\cf2 {chapterName}\b0\par}".Replace("{chapterName}", Chapter.chapters[chapterNumbers[i]].name);

                    box2.Document.SetText(TextSetOptions.FormatRtf, rtf1);

                    ITextRange range = box2.Document.GetRange(0, rtf1.Length);
                    range.Collapse(false);

                    string rtf2 = Chapter.chapters[chapterNumbers[i]].text;
                    range.SetText(TextSetOptions.FormatRtf, rtf2);

                    range.CharacterFormat.ForegroundColor = Colors.Black;
                    range.CharacterFormat.Size = 11;

                    box2.Document.GetText(TextGetOptions.FormatRtf, out txts[i]);
                }
                else
                {
                    txts[i] = Chapter.chapters[chapterNumbers[i]].text;
                }
            }

            box.Document.SetText(TextSetOptions.FormatRtf, txts[0]);

            for (int i = 1; i < chapterNumbers.Count; i++)
            {
                if (txts[i] != null)
                {
                    ITextRange range = box.Document.GetRange(0, txts[i - 1].Length);
                    range.Collapse(false);
                    range.SetText(TextSetOptions.FormatRtf, txts[i]);
                    
                    range.CharacterFormat.ForegroundColor = Colors.Black;
                    range.CharacterFormat.Size = 11;

                    box.Document.GetText(TextGetOptions.FormatRtf, out txts[i]);
                }
            }

            box.Document.SaveToStream(TextGetOptions.FormatRtf, await file.OpenAsync(FileAccessMode.ReadWrite));
        }
        #endregion

        #region Dialogues
        private static async Task ExportDialogues(StorageFolder folder, string fileName, string extension, List<int> chapterNumbers, List<string> characterDialogueNames)//přepracovat
        {
            string toExport = "";

            foreach (var chapterNumber in chapterNumbers)
            {
                RichEditBox box = new RichEditBox();
                box.Document.SetText(TextSetOptions.FormatRtf, Chapter.chapters[chapterNumber].text);
                box.Document.GetText(TextGetOptions.None, out string txt2);
                toExport += txt2;
            }

            if (extension == ".txt")
            {
                toExport += $"{Dialogue.FormatDialoguesToString(Dialogue.GetFromCharactersFromString(toExport, characterDialogueNames))}";
            }
            else
            if (extension == ".json")
            {
                var dialogues = new List<Dialogue>();
                dialogues.AddRange(Dialogue.GetFromCharactersFromString(toExport, characterDialogueNames));

                toExport = JsonSerializer.Serialize(dialogues, new JsonSerializerOptions() { WriteIndented = true });
            }

            try
            {
                var storageFile = await folder.CreateFileAsync($"{fileName}{extension}", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(storageFile, toExport); 
            }
            catch (Exception)
            {
                //notification system.notify
            }
        }
        #endregion

        #region Characters
        private static async Task ExportCharacters(StorageFolder folder, string fileName, string extension, List<Character> selectedCharacters)
        {
            string json = JsonSerializer.Serialize(selectedCharacters, new JsonSerializerOptions() { WriteIndented = true });

            try
            {
                var storageFile = await folder.CreateFileAsync($"{fileName}{extension}", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(storageFile, json);
            }
            catch (Exception)
            {
                //notification system.notify
            }
        }
        #endregion
    }
}
