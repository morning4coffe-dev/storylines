using Microsoft.UI.Xaml.Controls;
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
    }

    public sealed class ExportOperationResult
    {
        private ExportOperationResult(bool succeeded, string errorResourceKey, InfoBarSeverity notificationSeverity)
        {
            Succeeded = succeeded;
            ErrorResourceKey = errorResourceKey;
            NotificationSeverity = notificationSeverity;
        }

        public bool Succeeded { get; }
        public string ErrorResourceKey { get; }
        public InfoBarSeverity NotificationSeverity { get; }

        public static ExportOperationResult Success() => new ExportOperationResult(true, null, InfoBarSeverity.Informational);

        public static ExportOperationResult Failure(string errorResourceKey, InfoBarSeverity notificationSeverity = InfoBarSeverity.Error) =>
            new ExportOperationResult(false, errorResourceKey, notificationSeverity);
    }

    public interface IExportService
    {
        IReadOnlyList<ExportCapabilityDefinition> GetCapabilities();
        ExportCapabilityDefinition GetCapability(ExportTarget target);
        Task<ExportOperationResult> ExportAsync(ExportRequest request);
    }
}