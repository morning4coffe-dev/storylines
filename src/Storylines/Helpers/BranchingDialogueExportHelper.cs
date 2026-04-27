using Storylines.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Storylines.Helpers
{
    public static class BranchingDialogueExportHelper
    {
        public static string ConvertGraphToTwee(BranchingDialogueGraphData graph)
        {
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var nodeById = graph.Nodes.ToDictionary(n => n.Id ?? string.Empty, n => n);

            foreach (var node in graph.Nodes)
            {
                var passageName = !string.IsNullOrWhiteSpace(node.Title) ? node.Title : node.Id;
                var isStart = node.Id == graph.StartNodeId;

                var tags = new List<string>();
                if (node.Tags != null)
                    tags.AddRange(node.Tags);
                if (isStart)
                    tags.Add("start");

                var tagStr = tags.Count > 0 ? $" [{string.Join(" ", tags)}]" : string.Empty;

                var posStr = string.Empty;
                if (node.PositionX.HasValue && node.PositionY.HasValue)
                    posStr = $" {{\"position\":\"{(int)node.PositionX.Value},{(int)node.PositionY.Value}\"}}";

                sb.AppendLine($":: {passageName}{tagStr}{posStr}");

                if (!string.IsNullOrWhiteSpace(node.Speaker))
                    sb.AppendLine($"[speaker: {node.Speaker}]");

                if (!string.IsNullOrWhiteSpace(node.Text))
                    sb.AppendLine(node.Text);

                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (string.IsNullOrWhiteSpace(choice.TargetNodeId) || !nodeById.TryGetValue(choice.TargetNodeId, out var target))
                            continue;

                        var targetName = !string.IsNullOrWhiteSpace(target.Title) ? target.Title : target.Id;
                        var choiceText = !string.IsNullOrWhiteSpace(choice.Text) ? choice.Text : targetName;

                        if (choiceText == targetName)
                            sb.AppendLine($"[[{targetName}]]");
                        else
                            sb.AppendLine($"[[{choiceText}->{targetName}]]");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        public static string ConvertGraphToScreenplay(BranchingDialogueGraphData graph)
        {
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var nodeById = graph.Nodes.ToDictionary(n => n.Id ?? string.Empty, n => n);
            var visited = new HashSet<string>();
            var toVisit = new Queue<string>();

            if (!string.IsNullOrWhiteSpace(graph.StartNodeId))
                toVisit.Enqueue(graph.StartNodeId);

            while (toVisit.Count > 0)
            {
                var currentId = toVisit.Dequeue();
                if (!visited.Add(currentId) || !nodeById.TryGetValue(currentId, out var node))
                    continue;

                var speaker = !string.IsNullOrWhiteSpace(node.Speaker) ? node.Speaker.ToUpperInvariant() : "NARRATOR";
                sb.AppendLine($"{speaker}:");
                if (!string.IsNullOrWhiteSpace(node.Text))
                    sb.AppendLine($"  {node.Text}");
                sb.AppendLine();

                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (!string.IsNullOrWhiteSpace(choice.TargetNodeId))
                            toVisit.Enqueue(choice.TargetNodeId);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static BranchingDialogueGraphData ImportFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var graph = Newtonsoft.Json.JsonConvert.DeserializeObject<BranchingDialogueGraphData>(json);
                graph?.EnsureValid();
                return graph;
            }
            catch
            {
                return null;
            }
        }
    }
}
