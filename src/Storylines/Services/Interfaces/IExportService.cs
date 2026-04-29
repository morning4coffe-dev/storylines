using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Storylines.Services.Interfaces
{
    public sealed class ExportRequest
    {
        public ExportTarget Target { get; set; }
        public ExportFormatId FormatId { get; set; }
        public StorageFolder Folder { get; set; }
        public StorageFile File { get; set; }
        public string FileName { get; set; }
        public IReadOnlyList<int> ChapterIndexes { get; set; } = Array.Empty<int>();
        public IReadOnlyList<string> CharacterTokens { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> DialogueCharacterTokens { get; set; } = Array.Empty<string>();
        public bool IncludeChapterName { get; set; }
        public BranchingDialogueGraphData Graph { get; set; }
    }

    public sealed class ExportOperationResult
    {
        private ExportOperationResult(bool succeeded, string errorResourceKey)
        {
            Succeeded = succeeded;
            ErrorResourceKey = errorResourceKey;
        }

        public bool Succeeded { get; }
        public string ErrorResourceKey { get; }

        public static ExportOperationResult Success() => new ExportOperationResult(true, null);

        public static ExportOperationResult Failure(string errorResourceKey) =>
            new ExportOperationResult(false, errorResourceKey);
    }

    public interface IExportService
    {
        IReadOnlyList<ExportCapabilityDefinition> GetCapabilities();
        ExportCapabilityDefinition GetCapability(ExportTarget target);
        Task<ExportOperationResult> ExportAsync(ExportRequest request);
        BranchingDialogueGraphData ImportBranchingDialogueJson(string json);
    }
}