using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Models
{
    public enum ExportTarget
    {
        None,
        Chapters,
        Dialogues,
        Characters,
        BranchingDialogue
    }

    public enum ExportFormatId
    {
        PlainText,
        RichText,
        Markdown,
        Csv,
        Json,
        Twee,
        Screenplay
    }

    public enum ExportSelectionKind
    {
        None,
        Chapters,
        Characters
    }

    public sealed class ExportFormatDefinition
    {
        public ExportFormatDefinition(
            ExportFormatId id,
            string defaultExtension,
            IEnumerable<string> extensions = null,
            string menuTextResourceKey = null,
            string successMessageResourceKey = null)
        {
            if (string.IsNullOrWhiteSpace(defaultExtension))
                throw new ArgumentException("A default extension is required.", nameof(defaultExtension));

            var allExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                defaultExtension
            };

            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    if (!string.IsNullOrWhiteSpace(extension))
                        allExtensions.Add(extension);
                }
            }

            Id = id;
            DefaultExtension = defaultExtension;
            Extensions = allExtensions.ToArray();
            MenuTextResourceKey = menuTextResourceKey;
            SuccessMessageResourceKey = successMessageResourceKey;
        }

        public ExportFormatId Id { get; }
        public string DefaultExtension { get; }
        public IReadOnlyList<string> Extensions { get; }
        public string MenuTextResourceKey { get; }
        public string SuccessMessageResourceKey { get; }
    }

    public sealed class ExportCapabilityDefinition
    {
        public ExportCapabilityDefinition(
            ExportTarget target,
            ExportSelectionKind primarySelectionKind,
            string primarySelectionLabelResourceKey,
            bool supportsIncludeChapterName,
            bool showsSecondaryCharacterFilter,
            IEnumerable<ExportFormatDefinition> formats)
        {
            if (formats == null)
                throw new ArgumentNullException(nameof(formats));

            Target = target;
            PrimarySelectionKind = primarySelectionKind;
            PrimarySelectionLabelResourceKey = primarySelectionLabelResourceKey;
            SupportsIncludeChapterName = supportsIncludeChapterName;
            ShowsSecondaryCharacterFilter = showsSecondaryCharacterFilter;
            Formats = formats.ToArray();
        }

        public ExportTarget Target { get; }
        public ExportSelectionKind PrimarySelectionKind { get; }
        public string PrimarySelectionLabelResourceKey { get; }
        public bool SupportsIncludeChapterName { get; }
        public bool ShowsSecondaryCharacterFilter { get; }
        public IReadOnlyList<ExportFormatDefinition> Formats { get; }
    }

    public sealed class ExportSelectionState
    {
        public ExportSelectionState(string id, bool isSelected, int? index = null)
        {
            Id = id;
            IsSelected = isSelected;
            Index = index;
        }

        public string Id { get; }
        public bool IsSelected { get; }
        public int? Index { get; }
    }

    public sealed class ExportSelectionSnapshot
    {
        public static ExportSelectionSnapshot Empty { get; } = new ExportSelectionSnapshot(
            Array.Empty<int>(),
            Array.Empty<string>(),
            Array.Empty<string>());

        public ExportSelectionSnapshot(
            IReadOnlyList<int> chapterIndexes,
            IReadOnlyList<string> characterIds,
            IReadOnlyList<string> dialogueCharacterIds)
        {
            ChapterIndexes = chapterIndexes ?? Array.Empty<int>();
            CharacterIds = characterIds ?? Array.Empty<string>();
            DialogueCharacterIds = dialogueCharacterIds ?? Array.Empty<string>();
        }

        public IReadOnlyList<int> ChapterIndexes { get; }
        public IReadOnlyList<string> CharacterIds { get; }
        public IReadOnlyList<string> DialogueCharacterIds { get; }
    }

    public static class ExportSelectionBuilder
    {
        public static ExportSelectionSnapshot Build(
            ExportTarget target,
            IEnumerable<ExportSelectionState> primarySelections,
            IEnumerable<ExportSelectionState> secondarySelections = null)
        {
            var primary = (primarySelections ?? Array.Empty<ExportSelectionState>())
                .Where(item => item != null && item.IsSelected)
                .ToArray();

            var secondary = (secondarySelections ?? Array.Empty<ExportSelectionState>())
                .Where(item => item != null && item.IsSelected)
                .ToArray();

            return target switch
            {
                ExportTarget.Chapters => new ExportSelectionSnapshot(
                    primary.Where(item => item.Index.HasValue).Select(item => item.Index.Value).ToArray(),
                    Array.Empty<string>(),
                    Array.Empty<string>()),

                ExportTarget.Dialogues => new ExportSelectionSnapshot(
                    primary.Where(item => item.Index.HasValue).Select(item => item.Index.Value).ToArray(),
                    Array.Empty<string>(),
                    secondary.Where(item => !string.IsNullOrWhiteSpace(item.Id)).Select(item => item.Id).ToArray()),

                ExportTarget.Characters => new ExportSelectionSnapshot(
                    Array.Empty<int>(),
                    primary.Where(item => !string.IsNullOrWhiteSpace(item.Id)).Select(item => item.Id).ToArray(),
                    Array.Empty<string>()),

                _ => ExportSelectionSnapshot.Empty,
            };
        }
    }

    public static class ExportCapabilityCatalog
    {
        private static readonly IReadOnlyList<ExportCapabilityDefinition> _capabilities = new[]
        {
            new ExportCapabilityDefinition(
                ExportTarget.Chapters,
                ExportSelectionKind.Chapters,
                "exportChaptersToExport",
                supportsIncludeChapterName: true,
                showsSecondaryCharacterFilter: false,
                formats: new[]
                {
                    new ExportFormatDefinition(ExportFormatId.PlainText, ".txt"),
                    new ExportFormatDefinition(ExportFormatId.RichText, ".rtf"),
                    new ExportFormatDefinition(ExportFormatId.Markdown, ".md")
                }),

            new ExportCapabilityDefinition(
                ExportTarget.Dialogues,
                ExportSelectionKind.Chapters,
                "exportChaptersToExport",
                supportsIncludeChapterName: false,
                showsSecondaryCharacterFilter: true,
                formats: new[]
                {
                    new ExportFormatDefinition(ExportFormatId.PlainText, ".txt"),
                    new ExportFormatDefinition(ExportFormatId.Markdown, ".md"),
                    new ExportFormatDefinition(ExportFormatId.Csv, ".csv"),
                    new ExportFormatDefinition(ExportFormatId.Json, ".json")
                }),

            new ExportCapabilityDefinition(
                ExportTarget.Characters,
                ExportSelectionKind.Characters,
                "exportCharactersToExport",
                supportsIncludeChapterName: false,
                showsSecondaryCharacterFilter: false,
                formats: new[]
                {
                    new ExportFormatDefinition(ExportFormatId.Markdown, ".md"),
                    new ExportFormatDefinition(ExportFormatId.Json, ".json")
                }),

            new ExportCapabilityDefinition(
                ExportTarget.BranchingDialogue,
                ExportSelectionKind.None,
                null,
                supportsIncludeChapterName: false,
                showsSecondaryCharacterFilter: false,
                formats: new[]
                {
                    new ExportFormatDefinition(
                        ExportFormatId.Json,
                        ".json",
                        menuTextResourceKey: "branchingExportJson.Text",
                        successMessageResourceKey: "branchingExportedJson"),
                    new ExportFormatDefinition(
                        ExportFormatId.Twee,
                        ".twee",
                        new[] { ".tw" },
                        "branchingExportTwee.Text",
                        "branchingExportedTwee"),
                    new ExportFormatDefinition(
                        ExportFormatId.Screenplay,
                        ".txt",
                        menuTextResourceKey: "branchingExportScreenplay.Text",
                        successMessageResourceKey: "branchingExportedScreenplay")
                })
        };

        public static IReadOnlyList<ExportCapabilityDefinition> All => _capabilities;

        public static ExportCapabilityDefinition Find(ExportTarget target)
        {
            return _capabilities.FirstOrDefault(capability => capability.Target == target);
        }
    }
}