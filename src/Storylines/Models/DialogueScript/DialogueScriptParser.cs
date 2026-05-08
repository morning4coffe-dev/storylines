#if PRIVATE_PLUGINS
#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Storylines.Models.DialogueScript
{
    /// <summary>
    /// Parses the DialogueScript text format into a <see cref="BranchingDialogueGraphData"/> graph.
    /// Stateless — safe to reuse across calls.
    /// Text is the source of truth; the returned graph reflects only what the text contains.
    /// </summary>
    public class DialogueScriptParser
    {
        private static readonly Regex PosRegex = new Regex(
            @"@pos\s+x=(?<x>-?\d+(?:\.\d+)?)\s+y=(?<y>-?\d+(?:\.\d+)?)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public DialogueParseResult Parse(string chapterText, string chapterId)
        {
            var errors = new List<DialogueParseError>();
            var graph = new BranchingDialogueGraphData
            {
                Id = Guid.NewGuid().ToString(),
                ChapterId = chapterId,
                Nodes = new List<BranchingDialogueNodeData>()
            };

            if (string.IsNullOrEmpty(chapterText))
                return new DialogueParseResult(graph, errors);

            var normalized = chapterText.Replace("\r\n", "\n").Replace("\r", "\n");
            var rawLines = normalized.Split('\n');

            // If no node headers exist this is linear prose — return empty graph (spec rule 6).
            if (!rawLines.Any(l => IsTopLevelNodeHeader(l)))
                return new DialogueParseResult(graph, errors);

            BranchingDialogueNodeData? currentNode = null;
            BranchingDialogueChoiceData? currentChoice = null;

            for (int i = 0; i < rawLines.Length; i++)
            {
                var line = ClassifyLine(rawLines[i], i, errors);

                switch (line.Type)
                {
                    case DialogueLineType.Blank:
                        break;

                    case DialogueLineType.Comment:
                        // Capture @pos only on the line immediately after a node header
                        // (before any content has been added to that node).
                        if (currentNode != null
                            && currentNode.Speaker == null
                            && (currentNode.Choices == null || currentNode.Choices.Count == 0)
                            && (line.PositionX != 0f || line.PositionY != 0f))
                        {
                            currentNode.PositionX = line.PositionX;
                            currentNode.PositionY = line.PositionY;
                        }
                        break;

                    case DialogueLineType.NodeHeader:
                        // Skip the [end] sentinel — it is not a real node (spec rule 5).
                        if (string.Equals(line.NodeName, BranchingDialogueConstants.EndNodeId, StringComparison.Ordinal))
                            break;

                        currentNode = new BranchingDialogueNodeData
                        {
                            Id = line.NodeName,
                            Title = line.NodeName,
                            Tags = line.Tags != null ? line.Tags.ToList() : null!,
                            Choices = new List<BranchingDialogueChoiceData>(),
                            Actions = new List<BranchingDialogueActionData>()
                        };
                        graph.Nodes.Add(currentNode);
                        currentChoice = null;

                        // First node tagged #start becomes the start node.
                        if (graph.StartNodeId == null
                            && line.Tags != null
                            && Array.IndexOf(line.Tags, "start") >= 0)
                        {
                            graph.StartNodeId = currentNode.Id;
                        }
                        break;

                    case DialogueLineType.IndentedJump:
                        if (currentChoice != null)
                            currentChoice.TargetNodeId = line.TargetNodeId;
                        currentChoice = null;
                        break;

                    case DialogueLineType.SpeakerLine:
                        if (currentNode != null)
                        {
                            if (currentNode.Speaker == null)
                            {
                                currentNode.Speaker = line.SpeakerName;
                                currentNode.Text = line.SpeakerText;
                            }
                            else
                            {
                                // Multiple speaker lines in one node — append text.
                                currentNode.Text = (currentNode.Text ?? string.Empty)
                                    + "\n" + line.SpeakerText;
                            }
                        }
                        currentChoice = null;
                        break;

                    case DialogueLineType.Choice:
                        if (currentNode != null)
                        {
                            var choice = new BranchingDialogueChoiceData
                            {
                                Id = Guid.NewGuid().ToString(),
                                Text = line.ChoiceText ?? string.Empty,
                                TargetNodeId = line.TargetNodeId,
                                Conditions = new List<BranchingDialogueConditionData>()
                            };

                            if (!string.IsNullOrEmpty(line.ConditionFlag))
                            {
                                choice.Conditions.Add(new BranchingDialogueConditionData
                                {
                                    Flag = line.ConditionFlag,
                                    Operator = ConditionOperator.IsSet
                                });
                            }

                            currentNode.Choices!.Add(choice);
                            currentChoice = choice;
                        }
                        break;

                    case DialogueLineType.Condition:
                        // Standalone node-level condition — stored as notes.
                        // TODO: Define semantics for node-level (non-choice) conditions.
                        if (currentNode != null)
                            AppendNote(currentNode, rawLines[i].Trim());
                        currentChoice = null;
                        break;

                    case DialogueLineType.SetAction:
                        if (currentNode != null)
                        {
                            currentNode.Actions!.Add(new BranchingDialogueActionData
                            {
                                Flag = line.ActionFlag,
                                Value = line.ActionValue
                            });
                        }
                        currentChoice = null;
                        break;

                    case DialogueLineType.ClearAction:
                        if (currentNode != null)
                        {
                            currentNode.Actions!.Add(new BranchingDialogueActionData
                            {
                                Flag = line.ActionFlag,
                                Value = null
                            });
                        }
                        currentChoice = null;
                        break;

                    case DialogueLineType.Unknown:
                        if (currentNode != null)
                        {
                            var trimmed = rawLines[i].Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                                AppendNote(currentNode, trimmed);
                        }
                        break;
                }
            }

            // Set start node from first node if no #start tag was found.
            if (graph.StartNodeId == null && graph.Nodes.Count > 0)
                graph.StartNodeId = graph.Nodes[0].Id;

            return new DialogueParseResult(graph, errors);
        }

        // Returns true when a line is a top-level (non-indented) :: header that is not [end].
        private static bool IsTopLevelNodeHeader(string rawLine)
        {
            if (string.IsNullOrEmpty(rawLine) || rawLine[0] == ' ' || rawLine[0] == '\t')
                return false;

            var trimmed = rawLine.TrimStart();
            if (!trimmed.StartsWith("::", StringComparison.Ordinal))
                return false;

            var rest = trimmed.Substring(2).Trim();
            return !string.Equals(rest, BranchingDialogueConstants.EndNodeId, StringComparison.Ordinal)
                && !rest.StartsWith("[end]", StringComparison.Ordinal);
        }

        public DialogueScriptLine ClassifyLine(string raw, int lineIndex, List<DialogueParseError> errors)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Blank);

            var trimmed = raw.TrimStart();
            bool hasLeadingWhitespace = raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t');

            // IndentedJump — must check before NodeHeader.
            if (hasLeadingWhitespace && trimmed.StartsWith("::", StringComparison.Ordinal))
            {
                var target = trimmed.Substring(2).Trim();
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.IndentedJump,
                    TargetNodeId: NormalizeTarget(target));
            }

            // Comment (and optional @pos).
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                var posMatch = PosRegex.Match(trimmed);
                if (posMatch.Success
                    && float.TryParse(posMatch.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float px)
                    && float.TryParse(posMatch.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
                {
                    return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Comment,
                        PositionX: px, PositionY: py);
                }
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Comment);
            }

            // NodeHeader.
            if (trimmed.StartsWith("::", StringComparison.Ordinal))
                return ParseNodeHeader(raw, lineIndex, trimmed.Substring(2).Trim(), errors);

            // Choice.
            if (trimmed.StartsWith("->", StringComparison.Ordinal))
                return ParseChoice(raw, lineIndex, trimmed.Substring(2).Trim());

            // Standalone condition.
            if (trimmed.StartsWith("#if ", StringComparison.Ordinal))
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Condition,
                    ConditionFlag: trimmed.Substring(4).Trim());

            // @set action.
            if (trimmed.StartsWith("@set ", StringComparison.Ordinal))
            {
                var rest = trimmed.Substring(5).Trim();
                var eqIdx = rest.IndexOf('=');
                if (eqIdx >= 0)
                {
                    return new DialogueScriptLine(lineIndex, raw, DialogueLineType.SetAction,
                        ActionFlag: rest.Substring(0, eqIdx).Trim(),
                        ActionValue: rest.Substring(eqIdx + 1).Trim());
                }
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.SetAction,
                    ActionFlag: rest);
            }

            // @clear action.
            if (trimmed.StartsWith("@clear ", StringComparison.Ordinal))
                return new DialogueScriptLine(lineIndex, raw, DialogueLineType.ClearAction,
                    ActionFlag: trimmed.Substring(7).Trim());

            // SpeakerLine: prefix before first ':' must not contain ->, ::, #, @, //
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0)
            {
                var prefix = trimmed.Substring(0, colonIdx);
                if (!prefix.Contains("->") && !prefix.Contains("::")
                    && !prefix.Contains('#') && !prefix.Contains('@')
                    && !prefix.Contains("//")
                    && !string.IsNullOrWhiteSpace(prefix))
                {
                    return new DialogueScriptLine(lineIndex, raw, DialogueLineType.SpeakerLine,
                        SpeakerName: prefix.Trim(),
                        SpeakerText: trimmed.Substring(colonIdx + 1).Trim());
                }
            }

            return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Unknown);
        }

        private static DialogueScriptLine ParseNodeHeader(string raw, int lineIndex, string rest,
            List<DialogueParseError> errors)
        {
            string nodeName;
            string[]? tags = null;

            var bracketIdx = rest.IndexOf('[');
            if (bracketIdx >= 0)
            {
                nodeName = rest.Substring(0, bracketIdx).Trim();
                var tagSection = rest.Substring(bracketIdx + 1);
                var closingBracket = tagSection.IndexOf(']');
                if (closingBracket >= 0)
                    tagSection = tagSection.Substring(0, closingBracket);

                tags = tagSection.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.StartsWith("#"))
                    .Select(t => t.Substring(1))
                    .ToArray();
            }
            else
            {
                nodeName = rest.Trim();
            }

            // Spec rule: NodeName must not contain spaces (unless it is [end]).
            if (nodeName.Contains(' ')
                && !string.Equals(nodeName, BranchingDialogueConstants.EndNodeId, StringComparison.Ordinal))
            {
                errors.Add(new DialogueParseError(lineIndex,
                    $"Node name '{nodeName}' must not contain spaces.",
                    DialogueParseErrorSeverity.Error));
            }

            return new DialogueScriptLine(lineIndex, raw, DialogueLineType.NodeHeader,
                NodeName: nodeName, Tags: tags);
        }

        private static DialogueScriptLine ParseChoice(string raw, int lineIndex, string rest)
        {
            string? conditionFlag = null;
            string? targetNodeId = null;

            // Strip trailing #if condition first (rightmost wins if multiple, but spec only defines one).
            var ifIdx = rest.IndexOf(" #if ", StringComparison.Ordinal);
            if (ifIdx >= 0)
            {
                conditionFlag = rest.Substring(ifIdx + 5).Trim();
                rest = rest.Substring(0, ifIdx).Trim();
            }

            // Strip inline :: target.
            var jumpIdx = rest.IndexOf(" :: ", StringComparison.Ordinal);
            if (jumpIdx >= 0)
            {
                targetNodeId = NormalizeTarget(rest.Substring(jumpIdx + 4).Trim());
                rest = rest.Substring(0, jumpIdx).Trim();
            }
            else if (rest.EndsWith(" ::", StringComparison.Ordinal))
            {
                rest = rest.Substring(0, rest.Length - 3).Trim();
            }

            return new DialogueScriptLine(lineIndex, raw, DialogueLineType.Choice,
                ChoiceText: rest,
                TargetNodeId: targetNodeId,
                ConditionFlag: conditionFlag);
        }

        private static string NormalizeTarget(string target)
        {
            // [end] stays as the EndNodeId sentinel; everything else is a node name.
            return target;
        }

        private static void AppendNote(BranchingDialogueNodeData node, string text)
        {
            node.Notes = string.IsNullOrEmpty(node.Notes)
                ? text
                : node.Notes + "\n" + text;
        }
    }

    public record DialogueParseResult(
        BranchingDialogueGraphData Graph,
        IReadOnlyList<DialogueParseError> Errors
    );

    public record DialogueParseError(
        int LineIndex,
        string Message,
        DialogueParseErrorSeverity Severity
    );

    public enum DialogueParseErrorSeverity { Warning, Error }
}
#endif
