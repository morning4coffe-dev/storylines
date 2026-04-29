using Storylines.Models;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Storylines.Services
{
    public class ExportService : IExportService
    {
        private readonly IFileService _fileService;
        private readonly Interfaces.ILogger _logger;
        private readonly ProjectState _projectState;

        public ExportService(
            IFileService fileService = null,
            Interfaces.ILogger logger = null,
            ProjectState projectState = null)
        {
            _fileService = fileService ?? App.TryGetService<IFileService>() ?? new FileService();
            _logger = logger ?? App.TryGetService<Interfaces.ILogger>();
            _projectState = projectState ?? App.TryGetService<ProjectState>() ?? new ProjectState();
        }

        public enum WhatToExport { None, Chapters, Dialogues, Characters };
        public static WhatToExport export;

        public static async Task ExportAsync(StorageFolder folder, string fileName, string selectedExtension, List<int> chapterOrCharacterNumbers, List<Character> dialogueCharacters, bool withChapterName)
        {
            var service = App.TryGetService<IExportService>();
            if (service == null)
                return;

            var target = MapLegacyTarget(export);
            if (folder == null || target == ExportTarget.None)
                return;

            var request = new ExportRequest
            {
                Target = target,
                FormatId = ResolveFormatId(target, selectedExtension),
                Folder = folder,
                FileName = fileName,
                IncludeChapterName = withChapterName,
                ChapterIndexes = chapterOrCharacterNumbers?.ToArray() ?? Array.Empty<int>(),
                CharacterTokens = target == ExportTarget.Characters
                    ? dialogueCharacters?.Where(character => !string.IsNullOrWhiteSpace(character?.Token)).Select(character => character.Token).ToArray() ?? Array.Empty<string>()
                    : Array.Empty<string>(),
                DialogueCharacterTokens = target == ExportTarget.Dialogues
                    ? dialogueCharacters?.Where(character => !string.IsNullOrWhiteSpace(character?.Token)).Select(character => character.Token).ToArray() ?? Array.Empty<string>()
                    : Array.Empty<string>()
            };

            await service.ExportAsync(request);
        }

        /// <summary>Synchronous wrapper for callers that cannot await.</summary>
        public static void Export(StorageFolder folder, string fileName, string selectedExtension, List<int> chapterOrCharacterNumbers, List<Character> dialogueCharacters, bool withChapterName)
        {
            _ = ExportAsync(folder, fileName, selectedExtension, chapterOrCharacterNumbers, dialogueCharacters, withChapterName);
        }

        public IReadOnlyList<ExportCapabilityDefinition> GetCapabilities() => ExportCapabilityCatalog.All;

        public ExportCapabilityDefinition GetCapability(ExportTarget target) => ExportCapabilityCatalog.Find(target);

        public async Task<ExportOperationResult> ExportAsync(ExportRequest request)
        {
            if (request == null || request.Target == ExportTarget.None)
                return ExportOperationResult.Failure("exportFailedGeneric");

            switch (request.Target)
            {
                case ExportTarget.Chapters:
                    try
                    {
                        var chapterFile = await CreateDestinationFileAsync(request);
                        if (chapterFile == null)
                            return ExportOperationResult.Failure("exportFailedGeneric");

                        await ExportChaptersAsync(chapterFile, request.FormatId, request.ChapterIndexes, request.IncludeChapterName);
                        return ExportOperationResult.Success();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("Failed to export chapters", ex);
                        return ExportOperationResult.Failure("exportFailedGeneric");
                    }

                case ExportTarget.Dialogues:
                    try
                    {
                        var dialogueFile = await CreateDestinationFileAsync(request);
                        if (dialogueFile == null)
                            return ExportOperationResult.Failure("exportFailedGeneric");

                        await ExportDialoguesAsync(dialogueFile, request.FormatId, request.ChapterIndexes, request.DialogueCharacterTokens);
                        return ExportOperationResult.Success();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("Failed to export dialogues", ex);
                        return ExportOperationResult.Failure("exportFailedGeneric");
                    }

                case ExportTarget.Characters:
                    try
                    {
                        var characterFile = await CreateDestinationFileAsync(request);
                        if (characterFile == null)
                            return ExportOperationResult.Failure("exportFailedGeneric");

                        await ExportCharactersAsync(characterFile, request.CharacterTokens);
                        return ExportOperationResult.Success();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("Failed to export characters", ex);
                        return ExportOperationResult.Failure("exportFailedGeneric");
                    }

                case ExportTarget.BranchingDialogue:
                    try
                    {
                        var graphFile = await CreateDestinationFileAsync(request);
                        if (graphFile == null)
                            return ExportOperationResult.Failure("exportFailedGeneric");

                        await ExportBranchingDialogueAsync(graphFile, request.FormatId, request.Graph);
                        return ExportOperationResult.Success();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error("Failed to export branching dialogue", ex);
                        return ExportOperationResult.Failure("exportFailedGeneric");
                    }

                default:
                    return ExportOperationResult.Failure("exportFailedGeneric");
            }
        }

        public BranchingDialogueGraphData ImportBranchingDialogueJson(string json)
        {
            return Helpers.BranchingDialogueExportHelper.ImportFromJson(json);
        }

        #region Chapters
        private async Task ExportChaptersAsync(StorageFile file, ExportFormatId formatId, IReadOnlyList<int> chapterIndexes, bool withChapterName)
        {
            var selectedIndexes = (chapterIndexes ?? Array.Empty<int>())
                .Where(index => index >= 0 && index < _projectState.Chapters.Count)
                .ToArray();

            if (formatId == ExportFormatId.RichText)
            {
                await ExportChaptersToRtf(file, selectedIndexes, withChapterName);
                return;
            }

            var builder = new StringBuilder();
            var box = new RichEditBox();

            foreach (var chapterIndex in selectedIndexes)
            {
                box.Document.SetText(TextSetOptions.FormatRtf, _projectState.Chapters[chapterIndex].Text);
                box.Document.GetText(TextGetOptions.None, out string text);

                builder.Append(withChapterName ? $"{_projectState.Chapters[chapterIndex].Name}\n" : string.Empty);
                builder.Append(text);
                builder.Append('\n');
            }

            await _fileService.WriteAsync(file, builder.ToString());
        }

        private async Task ExportChaptersToRtf(StorageFile file, IReadOnlyList<int> chapterIndexes, bool withChapterName)
        {
            RichEditBox box = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };
            string[] txts = new string[chapterIndexes.Count];

            for (int i = 0; i < chapterIndexes.Count; i++)
            {
                if (withChapterName)
                {
                    RichEditBox box2 = new RichEditBox() { RequestedTheme = Windows.UI.Xaml.ElementTheme.Light };

                    string rtf1 = @"{\rtf1{\fonttbl{\f0 Segoe UI;}{\f1 Calibri;}{\f2 Verdana;}}{\colortbl;\red255\green255\blue255;\red0\green0\blue0;}\f0\b\cf2 {chapterName}\b0\par}".Replace("{chapterName}", _projectState.Chapters[chapterIndexes[i]].Name);

                    box2.Document.SetText(TextSetOptions.FormatRtf, rtf1);

                    ITextRange range = box2.Document.GetRange(0, rtf1.Length);
                    range.Collapse(false);

                    string rtf2 = _projectState.Chapters[chapterIndexes[i]].Text;
                    range.SetText(TextSetOptions.FormatRtf, rtf2);

                    range.CharacterFormat.ForegroundColor = Colors.Black;
                    range.CharacterFormat.Size = 11;

                    box2.Document.GetText(TextGetOptions.FormatRtf, out txts[i]);
                }
                else
                {
                    txts[i] = _projectState.Chapters[chapterIndexes[i]].Text;
                }
            }

            if (txts.Length == 0)
                return;

            box.Document.SetText(TextSetOptions.FormatRtf, txts[0]);

            for (int i = 1; i < chapterIndexes.Count; i++)
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
        private async Task ExportDialoguesAsync(StorageFile file, ExportFormatId formatId, IReadOnlyList<int> chapterIndexes, IReadOnlyList<string> dialogueCharacterTokens)
        {
            var selectedIndexes = (chapterIndexes ?? Array.Empty<int>())
                .Where(index => index >= 0 && index < _projectState.Chapters.Count)
                .ToArray();

            var selectedTokenSet = new HashSet<string>(dialogueCharacterTokens ?? Array.Empty<string>());
            var characterNames = _projectState.Characters
                .Where(character => selectedTokenSet.Contains(character.Token))
                .Select(character => character.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            string toExport = string.Empty;

            foreach (var chapterIndex in selectedIndexes)
            {
                RichEditBox box = new RichEditBox();
                box.Document.SetText(TextSetOptions.FormatRtf, _projectState.Chapters[chapterIndex].Text);
                box.Document.GetText(TextGetOptions.None, out string txt2);
                toExport += txt2;
            }

            if (formatId == ExportFormatId.PlainText)
            {
                toExport = Dialogue.FormatDialoguesToString(Dialogue.GetFromCharactersFromString(toExport, characterNames));
            }
            else if (formatId == ExportFormatId.Json)
            {
                var dialogues = new List<Dialogue>();
                dialogues.AddRange(Dialogue.GetFromCharactersFromString(toExport, characterNames));

                toExport = JsonSerializer.Serialize(dialogues, new JsonSerializerOptions() { WriteIndented = true });
            }

            await _fileService.WriteAsync(file, toExport);
        }
        #endregion

        #region Characters
        private async Task ExportCharactersAsync(StorageFile file, IReadOnlyList<string> characterTokens)
        {
            var selectedTokenSet = new HashSet<string>(characterTokens ?? Array.Empty<string>());
            var selectedCharacters = _projectState.Characters
                .Where(character => selectedTokenSet.Contains(character.Token))
                .ToList();

            string json = JsonSerializer.Serialize(selectedCharacters, new JsonSerializerOptions() { WriteIndented = true });

            await _fileService.WriteAsync(file, json);
        }
        #endregion

        #region Branching Dialogue Export

        private async Task ExportBranchingDialogueAsync(StorageFile file, ExportFormatId formatId, BranchingDialogueGraphData graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            string content = formatId switch
            {
                ExportFormatId.Json => Newtonsoft.Json.JsonConvert.SerializeObject(graph, Newtonsoft.Json.Formatting.Indented),
                ExportFormatId.Twee => Helpers.BranchingDialogueExportHelper.ConvertGraphToTwee(graph),
                ExportFormatId.Screenplay => Helpers.BranchingDialogueExportHelper.ConvertGraphToScreenplay(graph),
                _ => throw new InvalidOperationException($"Unsupported branching export format: {formatId}")
            };

            await _fileService.WriteAsync(file, content);
        }

        #endregion

        private async Task<StorageFile> CreateDestinationFileAsync(ExportRequest request)
        {
            if (request.File != null)
                return request.File;

            if (request.Folder == null || string.IsNullOrWhiteSpace(request.FileName))
                return null;

            var capability = GetCapability(request.Target);
            var format = capability?.Formats.FirstOrDefault(option => option.Id == request.FormatId);
            if (format == null)
                return null;

            return await request.Folder.CreateFileAsync(
                $"{request.FileName}{format.DefaultExtension}",
                CreationCollisionOption.ReplaceExisting);
        }

        private static ExportTarget MapLegacyTarget(WhatToExport target)
        {
            return target switch
            {
                WhatToExport.Chapters => ExportTarget.Chapters,
                WhatToExport.Dialogues => ExportTarget.Dialogues,
                WhatToExport.Characters => ExportTarget.Characters,
                _ => ExportTarget.None,
            };
        }

        private static ExportFormatId ResolveFormatId(ExportTarget target, string selectedExtension)
        {
            var capability = ExportCapabilityCatalog.Find(target);
            var format = capability?.Formats.FirstOrDefault(option =>
                option.Extensions.Any(extension => string.Equals(extension, selectedExtension, StringComparison.OrdinalIgnoreCase)));

            return format?.Id ?? ExportFormatId.PlainText;
        }
    }
}
