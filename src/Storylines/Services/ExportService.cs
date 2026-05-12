using Microsoft.UI.Text;
using System.Text;
using System.Text.Json;

namespace Storylines.Services;

public class ExportService : IExportService
{
    private readonly IFileService _fileService;
    private readonly Interfaces.ILogger _logger;
    private readonly ProjectState _projectState;

    private sealed class DialogueExportEntry
    {
        public string ChapterName { get; set; }
        public string CharacterName { get; set; }
        public string DialogueText { get; set; }
    }

    private sealed class ChapterExportEntry
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    public ExportService(
        IFileService fileService,
        Interfaces.ILogger logger,
        ProjectState projectState)
    {
        _fileService = fileService;
        _logger = logger;
        _projectState = projectState;
    }

    public enum WhatToExport { None, Chapters, Dialogues, Characters };
    public static WhatToExport export;

    public static async Task ExportAsync(StorageFolder folder, string fileName, string selectedExtension, List<int> chapterOrCharacterNumbers, List<Character> dialogueCharacters, bool withChapterName)
    {
        var service = App.TryGetService<IExportService>();
        if (service is null)
            return;

        var target = MapLegacyTarget(export);
        if (folder is null || target == ExportTarget.None)
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
        if (request is null || request.Target == ExportTarget.None)
            return ExportOperationResult.Failure("exportFailedGeneric");

        switch (request.Target)
        {
            case ExportTarget.Chapters:
                try
                {
                    var chapterFile = await CreateDestinationFileAsync(request);
                    if (chapterFile is null)
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
                    var buildResult = BuildDialoguesExportContent(request.FormatId, request.ChapterIndexes, request.DialogueCharacterTokens, out var dialogueContent);
                    if (!buildResult.Succeeded)
                        return buildResult;

                    var dialogueFile = await CreateDestinationFileAsync(request);
                    if (dialogueFile is null)
                        return ExportOperationResult.Failure("exportFailedGeneric");

                    await _fileService.WriteAsync(dialogueFile, dialogueContent);
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
                    if (characterFile is null)
                        return ExportOperationResult.Failure("exportFailedGeneric");

                    await ExportCharactersAsync(characterFile, request.FormatId, request.CharacterTokens);
                    return ExportOperationResult.Success();
                }
                catch (Exception ex)
                {
                    _logger?.Error("Failed to export characters", ex);
                    return ExportOperationResult.Failure("exportFailedGeneric");
                }

            default:
                return ExportOperationResult.Failure("exportFailedGeneric");
        }
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

        var chapters = selectedIndexes
            .Select(index => new ChapterExportEntry
            {
                Name = _projectState.Chapters[index].Name,
                Text = ExtractPlainText(_projectState.Chapters[index].Text)
            })
            .ToArray();

        var content = formatId switch
        {
            ExportFormatId.PlainText => FormatChaptersAsPlainText(chapters, withChapterName),
            ExportFormatId.Markdown => FormatChaptersAsMarkdown(chapters, withChapterName),
            _ => throw new InvalidOperationException($"Unsupported chapter export format: {formatId}")
        };

        await _fileService.WriteAsync(file, content);
    }

    private async Task ExportChaptersToRtf(StorageFile file, IReadOnlyList<int> chapterIndexes, bool withChapterName)
    {
        RichEditBox box = new RichEditBox() { RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light };
        string[] txts = new string[chapterIndexes.Count];

        for (int i = 0; i < chapterIndexes.Count; i++)
        {
            if (withChapterName)
            {
                RichEditBox box2 = new RichEditBox() { RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light };

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
            if (txts[i] is not null)
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
    private ExportOperationResult BuildDialoguesExportContent(ExportFormatId formatId, IReadOnlyList<int> chapterIndexes, IReadOnlyList<string> dialogueCharacterTokens, out string content)
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

        var entries = new List<DialogueExportEntry>();
        foreach (var chapterIndex in selectedIndexes)
        {
            var chapter = _projectState.Chapters[chapterIndex];
            var chapterText = ExtractPlainText(chapter.Text);
            var dialogues = Dialogue.GetFromCharactersFromString(chapterText, characterNames);

            entries.AddRange(dialogues.Select(dialogue => new DialogueExportEntry
            {
                ChapterName = chapter.Name,
                CharacterName = dialogue.Name,
                DialogueText = dialogue.Text,
            }));
        }

        if (entries.Count == 0)
        {
            content = string.Empty;
            return ExportOperationResult.Failure("exportNoDialoguesFound", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
        }

        var dialoguesToExport = entries.Select(entry => new Dialogue
        {
            Name = entry.CharacterName,
            Text = entry.DialogueText,
        }).ToList();

        content = formatId switch
        {
            ExportFormatId.PlainText => Dialogue.FormatDialoguesToString(dialoguesToExport),

            ExportFormatId.Markdown => FormatDialoguesAsMarkdown(entries),
            ExportFormatId.Csv => FormatDialoguesAsCsv(entries),
            ExportFormatId.Json => JsonSerializer.Serialize(dialoguesToExport, new JsonSerializerOptions() { WriteIndented = true }),
            _ => throw new InvalidOperationException($"Unsupported dialogue export format: {formatId}")
        };

        return ExportOperationResult.Success();
    }
    #endregion

    #region Characters
    private async Task ExportCharactersAsync(StorageFile file, ExportFormatId formatId, IReadOnlyList<string> characterTokens)
    {
        var selectedTokenSet = new HashSet<string>(characterTokens ?? Array.Empty<string>());
        var selectedCharacters = _projectState.Characters
            .Where(character => selectedTokenSet.Contains(character.Token))
            .ToList();

        var content = formatId switch
        {
            ExportFormatId.Json => JsonSerializer.Serialize(selectedCharacters, new JsonSerializerOptions() { WriteIndented = true }),
            ExportFormatId.Markdown => FormatCharactersAsMarkdown(selectedCharacters),
            _ => throw new InvalidOperationException($"Unsupported character export format: {formatId}")
        };

        await _fileService.WriteAsync(file, content);
    }
    #endregion

    private async Task<StorageFile> CreateDestinationFileAsync(ExportRequest request)
    {
        if (request.File is not null)
            return request.File;

        if (request.Folder is null || string.IsNullOrWhiteSpace(request.FileName))
            return null;

        var capability = GetCapability(request.Target);
        var format = capability?.Formats.FirstOrDefault(option => option.Id == request.FormatId);
        if (format is null)
            return null;

        return await request.Folder.CreateFileAsync(
            $"{request.FileName}{format.DefaultExtension}",
            CreationCollisionOption.ReplaceExisting);
    }

    private static string ExtractPlainText(string rtfContent)
    {
        var box = new RichEditBox();
        box.Document.SetText(TextSetOptions.FormatRtf, rtfContent ?? string.Empty);
        box.Document.GetText(TextGetOptions.None, out string text);
        return text ?? string.Empty;
    }

    private static string FormatChaptersAsPlainText(IEnumerable<ChapterExportEntry> chapters, bool withChapterName)
    {
        var builder = new StringBuilder();

        foreach (var chapter in chapters)
        {
            builder.Append(withChapterName ? $"{chapter.Name}\n" : string.Empty);
            builder.Append(chapter.Text);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatChaptersAsMarkdown(IEnumerable<ChapterExportEntry> chapters, bool withChapterName)
    {
        var builder = new StringBuilder();
        var hasWrittenChapter = false;

        foreach (var chapter in chapters)
        {
            if (hasWrittenChapter)
                builder.Append(withChapterName ? "\n\n" : "\n\n---\n\n");

            if (withChapterName && !string.IsNullOrWhiteSpace(chapter.Name))
            {
                builder.Append("# ");
                builder.AppendLine(chapter.Name);
                builder.AppendLine();
            }

            builder.Append((chapter.Text ?? string.Empty).TrimEnd());
            hasWrittenChapter = true;
        }

        return builder.ToString();
    }

    private string FormatDialoguesAsCsv(IReadOnlyList<DialogueExportEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",",
            EscapeCsvField(GetResourceOrFallback("chapters", "Chapters")),
            EscapeCsvField(GetResourceOrFallback("characters", "Characters")),
            EscapeCsvField(GetResourceOrFallback("dialoguesText", "Dialogues"))));

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(entry.ChapterName),
                EscapeCsvField(entry.CharacterName),
                EscapeCsvField(entry.DialogueText)));
        }

        return builder.ToString();
    }

    private static string FormatDialoguesAsMarkdown(IReadOnlyList<DialogueExportEntry> entries)
    {
        var builder = new StringBuilder();
        var groupedEntries = entries
            .GroupBy(entry => entry.ChapterName ?? string.Empty)
            .ToArray();

        for (int groupIndex = 0; groupIndex < groupedEntries.Length; groupIndex++)
        {
            var group = groupedEntries[groupIndex];
            if (!string.IsNullOrWhiteSpace(group.Key))
            {
                builder.Append("## ");
                builder.AppendLine(group.Key);
                builder.AppendLine();
            }

            foreach (var entry in group)
            {
                builder.Append("- **");
                builder.Append(entry.CharacterName);
                builder.Append(":** ");
                builder.AppendLine(entry.DialogueText);
            }

            if (groupIndex < groupedEntries.Length - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    private string FormatCharactersAsMarkdown(IReadOnlyList<Character> characters)
    {
        var builder = new StringBuilder();

        for (int index = 0; index < characters.Count; index++)
        {
            var character = characters[index];
            builder.Append("# ");
            builder.AppendLine(string.IsNullOrWhiteSpace(character.Name)
                ? GetResourceOrFallback("characters", "Characters")
                : character.Name);

            if (!string.IsNullOrWhiteSpace(character.DetailsLine))
            {
                builder.AppendLine();
                builder.AppendLine(character.DetailsLine);
            }

            if (!string.IsNullOrWhiteSpace(character.Description))
            {
                builder.AppendLine();
                builder.AppendLine(character.Description.Trim());
            }

            if (!string.IsNullOrWhiteSpace(character.Appearance))
            {
                builder.AppendLine();
                builder.AppendLine(character.Appearance.Trim());
            }

            if (index < characters.Count - 1)
                builder.AppendLine("\n---\n");
        }

        return builder.ToString();
    }

    private string GetResourceOrFallback(string key, string fallback)
    {
        var value = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string EscapeCsvField(string value)
    {
        var normalized = (value ?? string.Empty).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        return $"\"{normalized.Replace("\"", "\"\"")}\"";
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

#if PRIVATE_PLUGINS
    public BranchingDialogueGraphData ImportBranchingDialogueJson(string json)
    {
        return Storylines.Helpers.BranchingDialogueExportHelper.ImportFromJson(json);
    }
#endif
}
