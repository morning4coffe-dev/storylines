using System;
using Storylines.Models.Dialogue;

namespace Storylines.Services.Interfaces
{
    public interface IDialogueExportService
    {
        string ExportToJson(DialogueGraph graph);
        string ExportToPlainText(DialogueGraph graph);
    }
}
