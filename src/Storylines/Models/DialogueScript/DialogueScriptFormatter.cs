#if PRIVATE_PLUGINS
#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Storylines.Models.DialogueScript
{
    /// <summary>
    /// Serialises a <see cref="BranchingDialogueGraphData"/> back to DialogueScript text,
    /// and supports surgical single-node patching of existing text.
    /// </summary>
    public class DialogueScriptFormatter
    {
        /// <summary>
        /// Formats the full graph as a DialogueScript string.
        /// </summary>
        public string Format(BranchingDialogueGraphData? graph)
        {
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("// This file is both generated and manually editable");

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                if (node == null) continue;

                sb.AppendLine();
                AppendNode(sb, node);
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        /// <summary>
        /// Replaces only the section belonging to <paramref name="node"/> inside
        /// <paramref name="originalText"/>, leaving every other node untouched.
        /// Returns <paramref name="originalText"/> unchanged if the node cannot be found.
        /// </summary>
        public string PatchNode(string originalText, BranchingDialogueNodeData? node)
        {
            if (node == null)
                return originalText ?? string.Empty;
            if (string.IsNullOrEmpty(originalText))
                return originalText ?? string.Empty;

            var normalized = originalText.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split('\n');
            var nodeName = node.Title ?? node.Id ?? string.Empty;

            int sectionStart = -1;
            int sectionEnd = lines.Length;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Only match top-level (non-indented) node headers.
                if (lines[i].Length > 0 && lines[i][0] != ' ' && lines[i][0] != '\t'
                    && trimmed.StartsWith("::", StringComparison.Ordinal))
                {
                    var headerName = ParseHeaderName(trimmed.Substring(2).Trim());

                    if (string.Equals(headerName, nodeName, StringComparison.Ordinal))
                    {
                        sectionStart = i;
                    }
                    else if (sectionStart >= 0)
                    {
                        sectionEnd = i;
                        break;
                    }
                }
            }

            if (sectionStart < 0)
                return originalText; // node not found — return unchanged

            // Build replacement: trim trailing blank lines from the old section so the
            // gap between nodes is restored to exactly one blank line by the caller.
            var newNodeSb = new StringBuilder();
            AppendNode(newNodeSb, node);
            var newNodeText = newNodeSb.ToString().TrimEnd('\n', '\r');

            var beforeLines = lines.Take(sectionStart).ToArray();
            var afterLines = lines.Skip(sectionEnd).ToArray();

            var result = new StringBuilder();
            if (beforeLines.Length > 0)
            {
                result.Append(string.Join("\n", beforeLines));
                result.Append('\n');
            }
            result.Append(newNodeText);
            if (afterLines.Length > 0)
            {
                result.Append('\n');
                result.Append(string.Join("\n", afterLines));
            }

            var usesCrlf = originalText.Contains("\r\n");
            return usesCrlf
                ? result.ToString().Replace("\n", "\r\n")
                : result.ToString();
        }

        // Writes a single node (header + body) to the builder.  No leading blank line.
        private static void AppendNode(StringBuilder sb, BranchingDialogueNodeData node)
        {
            // -- Header --
            var header = new StringBuilder("::");
            header.Append(' ');
            header.Append(node.Title ?? node.Id ?? "UnnamedNode");

            if (node.Tags != null && node.Tags.Count > 0)
            {
                header.Append(" [");
                header.Append(string.Join(" ", node.Tags.Select(t => "#" + t)));
                header.Append(']');
            }
            sb.AppendLine(header.ToString());

            // -- Position comment --
            if (node.PositionX.HasValue || node.PositionY.HasValue)
            {
                var px = (node.PositionX ?? 0).ToString("G", CultureInfo.InvariantCulture);
                var py = (node.PositionY ?? 0).ToString("G", CultureInfo.InvariantCulture);
                sb.AppendLine($"// @pos x={px} y={py}");
            }

            // -- Speaker / text --
            if (!string.IsNullOrWhiteSpace(node.Speaker) && !string.IsNullOrWhiteSpace(node.Text))
                sb.AppendLine($"{node.Speaker}: {node.Text}");
            else if (!string.IsNullOrWhiteSpace(node.Text))
                sb.AppendLine(node.Text);

            // -- Actions --
            if (node.Actions != null)
            {
                foreach (var action in node.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.Flag)) continue;
                    sb.AppendLine(action.Value == null
                        ? $"@clear {action.Flag}"
                        : $"@set {action.Flag} = {action.Value}");
                }
            }

            // -- Choices --
            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    if (choice == null) continue;
                    AppendChoice(sb, choice);
                }
            }
        }

        private static void AppendChoice(StringBuilder sb, BranchingDialogueChoiceData choice)
        {
            var line = new StringBuilder("-> ");
            line.Append(choice.Text ?? string.Empty);

            // Emit inline IsSet condition (spec: -> text #if flag).
            var cond = choice.Conditions?.FirstOrDefault(
                c => c != null && !string.IsNullOrWhiteSpace(c.Flag) && c.Operator == ConditionOperator.IsSet);
            if (cond != null)
                line.Append($" #if {cond.Flag}");

            sb.AppendLine(line.ToString());

            // Emit jump target on its own indented line.
            if (!string.IsNullOrWhiteSpace(choice.TargetNodeId))
                sb.AppendLine($"    :: {choice.TargetNodeId}");
        }

        private static string ParseHeaderName(string rest)
        {
            var bracketIdx = rest.IndexOf('[');
            return bracketIdx >= 0
                ? rest.Substring(0, bracketIdx).Trim()
                : rest.Trim();
        }
    }
}
#endif
