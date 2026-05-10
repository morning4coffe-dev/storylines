using System;
using System.Linq;
using System.Text.Json;
using System.Text;
using Storylines.Models.Dialogue;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    public class DialogueExportService : IDialogueExportService
    {
        public string ExportToJson(DialogueGraph graph)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(graph, options);
        }

        public string ExportToPlainText(DialogueGraph graph)
        {
            if (graph == null) return string.Empty;

            var sb = new StringBuilder();

            // For simple linear plain text output, we'll just print nodes and their outgoing choices.
            foreach (var node in graph.Nodes)
            {
                sb.AppendLine($"[Node: {node.Id}, Speaker: {node.Speaker}]");
                sb.AppendLine(node.ContentPlainText);

                var outgoingChoices = graph.Choices.Where(c => c.SourceNodeId == node.Id).ToList();
                foreach (var choice in outgoingChoices)
                {
                    sb.AppendLine($"* Choice: \"{choice.ChoiceText}\" -> Node {choice.TargetNodeId}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
