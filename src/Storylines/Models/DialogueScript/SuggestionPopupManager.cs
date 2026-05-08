#nullable enable annotations
using System;

namespace Storylines.Models.DialogueScript
{
    public enum SuggestionTriggerType
    {
        None,
        /// <summary>The caret is positioned where the user is typing a node reference (after <c>::</c>).</summary>
        NodeReference,
        /// <summary>The caret is positioned where the user is typing a tag (after <c>#</c> inside a node header bracket).</summary>
        TagReference,
        /// <summary>The caret is positioned at the start of a line where a speaker name is being typed (before the first <c>:</c>).</summary>
        SpeakerReference
    }

    public record SuggestionPopupResult(
        bool ShouldShow,
        SuggestionTriggerType TriggerType,
        string FilterText,
        int TriggerStart,
        int TriggerEnd
    )
    {
        public static SuggestionPopupResult None { get; } =
            new SuggestionPopupResult(false, SuggestionTriggerType.None, string.Empty, 0, 0);
    }

    /// <summary>
    /// Pure, stateless analyser used by <c>DialogueTextEditor</c> to decide when to show the
    /// suggestion popup and what filter text to apply.  Has no UI / framework dependencies and
    /// is safe to test in isolation.
    /// </summary>
    public sealed class SuggestionPopupManager
    {
        /// <summary>
        /// Inspects <paramref name="text"/> at <paramref name="caretPosition"/> and returns a
        /// <see cref="SuggestionPopupResult"/> describing the trigger context (if any).
        /// The caller is responsible for sourcing the actual suggestion items (node names,
        /// speakers, tags) and filtering them by <see cref="SuggestionPopupResult.FilterText"/>.
        /// </summary>
        public SuggestionPopupResult Analyze(string text, int caretPosition)
        {
            if (string.IsNullOrEmpty(text) || caretPosition < 0 || caretPosition > text.Length)
                return SuggestionPopupResult.None;

            // Find the start of the current line.
            var lineStart = caretPosition;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            var line = text.Substring(lineStart, caretPosition - lineStart);

            // Node reference: line consists of `::` (optionally indented + `-> ... :: `) followed by partial name.
            // Examples that should trigger:
            //   "    :: Mer"          → NodeReference, filter "Mer"
            //   "-> Hi. :: Re"        → NodeReference, filter "Re"
            //   ":: "                 → suppressed (top-level header — user is naming a NEW node)
            var nodeRefStart = FindNodeReferenceTriggerStart(line);
            if (nodeRefStart >= 0)
            {
                var partial = line.Substring(nodeRefStart);
                return new SuggestionPopupResult(
                    ShouldShow: true,
                    TriggerType: SuggestionTriggerType.NodeReference,
                    FilterText: partial,
                    TriggerStart: lineStart + nodeRefStart,
                    TriggerEnd: caretPosition);
            }

            // Tag reference: inside `:: NodeName [#par`  — caret is after a `#` inside the bracketed tag list.
            var tagRefStart = FindTagReferenceTriggerStart(line);
            if (tagRefStart >= 0)
            {
                var partial = line.Substring(tagRefStart);
                return new SuggestionPopupResult(
                    ShouldShow: true,
                    TriggerType: SuggestionTriggerType.TagReference,
                    FilterText: partial,
                    TriggerStart: lineStart + tagRefStart,
                    TriggerEnd: caretPosition);
            }

            // Speaker reference: caret is at the start of a line, line is non-empty, no colon yet,
            // and line doesn't start with any structural prefix (::, ->, //, #, @, whitespace).
            if (IsSpeakerReferenceLine(line))
            {
                return new SuggestionPopupResult(
                    ShouldShow: true,
                    TriggerType: SuggestionTriggerType.SpeakerReference,
                    FilterText: line,
                    TriggerStart: lineStart,
                    TriggerEnd: caretPosition);
            }

            return SuggestionPopupResult.None;
        }

        // Returns the offset (within `line`) where the partial node-reference name begins,
        // or -1 if the caret is not in a node-reference context.
        private static int FindNodeReferenceTriggerStart(string line)
        {
            // Indented jump: leading whitespace + "::" + space + partial.
            // Top-level "::" (no leading whitespace) is suppressed — the user is naming a NEW node,
            // not referencing an existing one.
            var trimmedStart = 0;
            while (trimmedStart < line.Length && (line[trimmedStart] == ' ' || line[trimmedStart] == '\t'))
                trimmedStart++;

            bool isIndented = trimmedStart > 0;

            if (isIndented
                && trimmedStart + 1 < line.Length
                && line[trimmedStart] == ':' && line[trimmedStart + 1] == ':')
            {
                // After `::` skip optional space, partial name = rest of line.
                var afterColons = trimmedStart + 2;
                while (afterColons < line.Length && line[afterColons] == ' ')
                    afterColons++;
                return afterColons;
            }

            // Inline jump: `-> ... :: <partial>` somewhere on a `->` line.
            if (line.StartsWith("-> ", StringComparison.Ordinal))
            {
                var jumpIdx = line.LastIndexOf(" :: ", StringComparison.Ordinal);
                if (jumpIdx >= 0)
                {
                    var afterColons = jumpIdx + 4;
                    return afterColons;
                }
            }

            return -1;
        }

        // Returns the offset of the partial tag (after the most-recent `#` inside `[...]`).
        private static int FindTagReferenceTriggerStart(string line)
        {
            // Must be on a node-header line: starts with `::` and contains `[` before the caret.
            if (!line.StartsWith("::", StringComparison.Ordinal))
                return -1;

            var bracketIdx = line.IndexOf('[');
            if (bracketIdx < 0)
                return -1;

            // Find the most recent `#` after `[` and before caret.
            var hashIdx = line.LastIndexOf('#');
            if (hashIdx <= bracketIdx)
                return -1;

            // Reject if the partial contains whitespace (user has moved past the tag).
            for (int i = hashIdx + 1; i < line.Length; i++)
            {
                if (line[i] == ' ' || line[i] == '\t' || line[i] == ']')
                    return -1;
            }

            return hashIdx + 1; // skip the # itself
        }

        // True when the line looks like the user is starting to type a speaker name.
        // Heuristic: non-empty, no leading whitespace, no `:` yet, not starting with structural prefix.
        private static bool IsSpeakerReferenceLine(string line)
        {
            if (line.Length == 0)
                return false;
            if (line[0] == ' ' || line[0] == '\t')
                return false;
            if (line.IndexOf(':') >= 0)
                return false;

            // Reject structural prefixes.
            if (line.StartsWith("->", StringComparison.Ordinal)
                || line.StartsWith("//", StringComparison.Ordinal)
                || line[0] == '#'
                || line[0] == '@')
            {
                return false;
            }

            // Allow alphanumerics and a small set of name-like characters.
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '\'' && c != '-' && c != '.')
                    return false;
            }

            return true;
        }
    }
}
