using Storylines.Components.DialogueWindows;
using Storylines.Components.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Printing;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Printing;

namespace Storylines.Components
{
    public class SaveSystem
    {
        public static Dictionary<string, string> savedValues = new Dictionary<string, string>();

        public static StorageFile saveFile;

        private static readonly ChapterListComponent chpt = MainPage.chapterList;

        public static string loadedProjectName = "AStoryWithNoName.srl";

        #region Save
        private enum AfterSave { DoNothing, ClearEverything, Exit };
        private AfterSave afterSave;

        public void Save()
        {
            afterSave = AfterSave.DoNothing;

            if (saveFile != null)
            {
                WriteToFile(GetSaveValues());
                MainPage.mainPage.unSavedProgress = false;
            }
            else
            {
                SaveDialogue.Open();
            }
        }

        public void SaveAs()
        {
            SaveDialogue.Open();
        }

        public void SaveAndExitOrClearAll(bool exit)
        {
            if (exit)
            {
                afterSave = AfterSave.Exit;
            }
            else
            {
                afterSave = AfterSave.ClearEverything;
            }

            if (saveFile != null)
            {
                WriteToFile(GetSaveValues());
                MainPage.mainPage.unSavedProgress = false;
            }
            else
            {
                SaveDialogue.Open();
            }
        }

        private string GetSaveValues()
        {
            savedValues.Clear();
            savedValues.Add("version", $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}");
            savedValues.Add("lastOpenedChapter", $"{MainPage.chapterList.chaptersListView.SelectedIndex}");

            for (int i = 0; i < Characters.characters.Count; i++)
            {
                savedValues.Add($"character{i}", $"{Characters.characters[i].name}<Y&⨝m>{Characters.characters[i].description}");
            }

            for (int i = 0; i < chpt.chapters.Count; i++)
            {
                if (chpt.chapters[i].text.Contains("<Y&⨝m>") || chpt.chapters[i].text.Contains(">[Y≇g&<") || chpt.chapters[i].text.Contains("@N*∛$\n"))
                {
                    chpt.chapters[i].text.Replace("<Y&⨝m>", "");
                    chpt.chapters[i].text.Replace(">[Y≇g&<", "");
                    chpt.chapters[i].text.Replace("@N*∛$\n", "");
                }

                savedValues.Add($"chapter{i}", $"{chpt.chapters[i].name}<Y&⨝m>{chpt.chapters[i].text}");
            }

            string[] valuesToSave = savedValues.Values.ToArray();
            string[] keysToSave = savedValues.Keys.ToArray();
            string toSave = "";

            for (int i = 0; i < savedValues.Count; i++)
            {
                toSave += $"{keysToSave[i]}>[Y≇g&<{valuesToSave[i]}@N*∛$\n";
            }

            return toSave;
        }

        private async void NewFile(StorageFolder folder, string fileContent, string fileName)
        {
            var ticketsFile = await folder.CreateFileAsync($@"{fileName}.srl", CreationCollisionOption.ReplaceExisting);

            //DocumentProperties documentProperties = await ticketsFile.Properties.GetDocumentPropertiesAsync();
            //documentProperties.Keywords.Add("24.4");
            //await documentProperties.SavePropertiesAsync();

            saveFile = ticketsFile;
            ProjectFile.New(ticketsFile);

            WriteToFile(fileContent);
        }

        private async void WriteToFile(string fileContent)
        {
            try
            {
                await FileIO.WriteTextAsync(saveFile, fileContent);

                ToDoAfterSave();
            }
            catch
            {
                var dialog = new MessageDialog("Run into a problem.", "Your file could not be saved.");
                dialog.Commands.Add(new UICommand("Ok", delegate (IUICommand command) { }));

                await dialog.ShowAsync();
            }
        }

        private void ToDoAfterSave()
        {
            switch (afterSave)
            {
                case AfterSave.DoNothing:
                    loadedProjectName = saveFile.Name;
                    MainPage.mainPage.unSavedProgress = false;
                    MainPage.mainPage.UpdateTitleBar();
                    break;
                case AfterSave.ClearEverything:
                    saveFile = null;
                    MainPage.mainPage.ClearEverything();
                    MainPage.mainPage.unSavedProgress = false;
                    LoadFileDialogue.Open();
                    break;
                case AfterSave.Exit:
                    App.Current.Exit();
                    break;
            }
        }

        public async void OpenFileExplorerSaveAsync(string fileName)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                NewFile(folder, GetSaveValues(), fileName);
            }
            else
            {
                var dialog = new MessageDialog("Run into a problem.", "Your file could not be saved.");
                dialog.Commands.Add(new UICommand("Ok", delegate (IUICommand command) { }));

                await dialog.ShowAsync();
            }
        }
        #endregion

        #region Load
        public async Task Load(StorageFile file)
        {
            try
            {
                MainPage.mainPage.ClearEverything();
                LoadFileDialogue.loadFile.isEscape = false;
                LoadFileDialogue.loadFile.Hide();

                string savedTickets = await FileIO.ReadTextAsync(file);
                string[] loadedStrings = savedTickets.Split("@N*∛$\n", StringSplitOptions.RemoveEmptyEntries);

                savedValues.Clear();
                for (int i = 0; i < loadedStrings.Length; i++)
                {
                    string[] values = loadedStrings[i].Split(">[Y≇g&<", StringSplitOptions.RemoveEmptyEntries);
                    savedValues.Add(values[0], values[1]);
                }

                for (int i = 0; i < savedValues.Keys.Count; i++)
                {
                    if (savedValues.Keys.ToList()[i].Contains("character"))
                    {
                        var str = savedValues.Values.ToList()[i].Split("<Y&⨝m>", StringSplitOptions.None);
                        Character.CreateNew(str[0], str[1]);
                    }
                }

                for (int i = 0; i < savedValues.Keys.Count; i++)
                {
                    if (savedValues.Keys.ToList()[i].Contains("chapter"))
                    {
                        var str = savedValues.Values.ToList()[i].Split("<Y&⨝m>", StringSplitOptions.None);
                        Chapter.AddExisting(str[0], Guid.NewGuid().ToString(), str[1]);
                    }
                }

                LoadVariables();

                saveFile = file;

                loadedProjectName = file.Name;
                MainPage.mainPage.unSavedProgress = false;
                MainPage.mainPage.UpdateTitleBar();
                //Windows.Security.Cryptography.CryptographicBuffer.ConvertBinaryToString
            }
            catch
            {
                MessageDialog dialog = new MessageDialog("Run into a problem.", "Your file could not be loaded.");
                dialog.Commands.Add(new UICommand("Ok", delegate (IUICommand command) { }));

                _ = await dialog.ShowAsync();
            }
        }

        private void LoadVariables()
        {
            try
            {
                MainPage.chapterList.chaptersListView.SelectedIndex = Convert.ToInt32(savedValues["lastOpenedChapter"]);
            }
            catch { }
        }

        public async void OpenFileEplorerLoadAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".srl");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                if (!ProjectFile.ChectIfProjectExists(file))
                {
                    ProjectFile.New(file);
                }

                _ = Load(file);
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Run into a problem.", "Your file could not be loaded.");
                dialog.Commands.Add(new UICommand("Ok", delegate (IUICommand command) { }));

                _ = await dialog.ShowAsync();
            }
        }

        public async void DefaultLaunch(IStorageItem storageItem)
        {
            await MainPage.saveSystem.Load(storageItem as StorageFile);
        }
        #endregion

        #region Export
        public async void Export(List<int> chapterNumbers, bool withChapterName)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };

            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.FileTypeChoices.Add("Rich Text Format", new List<string>() { ".rtf" });
            savePicker.SuggestedFileName = "My story";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);

                if (file.FileType == ".txt")
                {
                    _ = ExportToTxtAsync(file, chapterNumbers, withChapterName);
                }
                else
                if (file.FileType == ".rtf")
                {
                    _ = ExportToRtf(file, chapterNumbers, withChapterName);
                }
            }
        }

        private async Task ExportToTxtAsync(StorageFile file, List<int> chapterNumbers, bool withChapterName)
        {
            string toExport = "";
            RichEditBox box = new RichEditBox();

            for (int i = 0; i < chapterNumbers.Count; i++)
            {
                box.Document.SetText(TextSetOptions.FormatRtf, chpt.chapters[chapterNumbers[i]].text);
                box.Document.GetText(TextGetOptions.None, out string txt);

                toExport += $"{(withChapterName ? $"{chpt.chapters[chapterNumbers[i]].name}\n" : string.Empty)}{txt}\n";
            }

            await FileIO.WriteTextAsync(file, toExport);
        }

        private async Task ExportToRtf(StorageFile file, List<int> chapterNumbers, bool withChapterName)
        {
            RichEditBox box = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };
            string[] txts = new string[chapterNumbers.Count];

            for (int i = 0; i < chapterNumbers.Count; i++)
            {
                if (withChapterName)
                {
                    RichEditBox box2 = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };

                    string rtf1 = @"{\rtf1{\fonttbl{\f0 Segoe UI;}{\f1 Calibri;}{\f2 Verdana;}}{\colortbl;\red255\green255\blue255;\red0\green0\blue0;}\f0\b\cf2 {chapterName}\b0\par}".Replace("{chapterName}", MainPage.chapterList.chapters[chapterNumbers[i]].name);

                    box2.Document.SetText(TextSetOptions.FormatRtf, rtf1);

                    ITextRange range = box2.Document.GetRange(0, rtf1.Length);
                    range.Collapse(false);

                    string rtf2 = MainPage.chapterList.chapters[chapterNumbers[i]].text;
                    range.SetText(TextSetOptions.FormatRtf, rtf2);

                    range.CharacterFormat.ForegroundColor = Colors.Black;
                    range.CharacterFormat.Size = 11;

                    box2.Document.GetText(TextGetOptions.FormatRtf, out txts[i]);
                }
                else
                {
                    txts[i] = MainPage.chapterList.chapters[chapterNumbers[i]].text;
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

        #region Print
        private PrintManager printManager;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;

        public void Register()
        {
            printManager = PrintManager.GetForCurrentView();
            printManager.PrintTaskRequested += PrintTaskRequested;

            printDoc = new PrintDocument();
            printDocSource = printDoc.DocumentSource;
            printDoc.Paginate += Paginate;
            printDoc.GetPreviewPage += GetPreviewPage;
            printDoc.AddPages += AddPages;

            printDoc.AddPage(ChapterToPrint.NewPage("Hello there", "Hello guys"));
            printDoc.AddPagesComplete();
        }

        public async void Print()
        {
            if (PrintManager.IsSupported())
            {
                try
                {
                    Register();
                    await PrintManager.ShowPrintUIAsync();
                }
                catch
                {
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        Title = "Printing error",
                        Content = "\nSorry, printing can't proceed at this time.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                }
            }
            else
            {
                // Printing is not supported on this device
                ContentDialog noPrintingDialog = new ContentDialog()
                {
                    Title = "Printing not supported",
                    //CornerRadius = 6,
                    Content = "\nSorry, printing is not supported on this device.",
                    PrimaryButtonText = "OK"
                };
                await noPrintingDialog.ShowAsync();
            }
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);
            printTask.Completed += PrintTaskCompleted;
        }

        private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args)
        {
            args.SetSource(printDocSource);
        }

        private void Paginate(object sender, PaginateEventArgs e)
        {
            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            printDoc.SetPreviewPage(e.PageNumber, new ChapterToPrint());
        }

        private void AddPages(object sender, AddPagesEventArgs e)
        {
            printDoc.AddPage(new ChapterToPrint());

            // Indicate that all of the print pages have been provided
            printDoc.AddPagesComplete();
        }

        private async void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            // Notify the user when the print operation fails.
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    ContentDialog noPrintingDialog = new ContentDialog()
                    {
                        Title = "Printing error",
                        Content = "\nSorry, failed to print.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                });
            }
        }
        #endregion
    }

    public class ProjectFile
    {
        public string name;
        public string token;
        public string path;
        public StorageFile file;

        public static List<ProjectFile> projectFiles = new List<ProjectFile>();

        public static void New(StorageFile file)
        {
            projectFiles.Add(new ProjectFile() { name = file.Name, token = Remember(file), path = file.Path });
        }

        public static string Remember(StorageFile file)
        {
            string token = Guid.NewGuid().ToString();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, file);
            return token;
        }

        public static void Remove(string token)
        {
            for (int i = 0; i < projectFiles.Count; i++)
            {
                if (projectFiles[i].token == token)
                {
                    projectFiles.RemoveAt(i);
                    StorageApplicationPermissions.FutureAccessList.Remove(token);
                }
            }
        }

        public static async Task LoadAllAsync()
        {
            foreach (AccessListEntry token in StorageApplicationPermissions.FutureAccessList.Entries)
            {
                int timeout = 600;
                Task<StorageFile> task = GetProjectFromTokenAsync(token.Token);

                if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                {
                    StorageFile pf = task.Result;
                    projectFiles.Add(new ProjectFile() { name = pf.Name, token = token.Token, path = pf.Path, file = pf });
                }
                else
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(token.Token);
                }
            }
         }

        public static async Task<StorageFile> GetProjectFromTokenAsync(string token)
        {
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(token)) return null;
            return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
        }

        public static bool ChectIfProjectExists(StorageFile file)
        {
            for (int i = 0; i < projectFiles.Count; i++)
            {
                if (projectFiles[i].path == file.Path)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
