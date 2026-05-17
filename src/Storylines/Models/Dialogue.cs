
namespace Storylines.Models
{
    /// <summary>
    /// Represents a single dialogue entry that can be parsed from chapter text.
    /// </summary>
    public class Dialogue
    {
        private static readonly Regex LegacyStructuredDialogueRegex = new Regex(@"\{name=(?<name>[^;{}]+);\s*text=""(?<text>.*?)""\}", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Gets or sets the speaking character name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the spoken dialogue text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Creates a new dialogue placeholder using the unified writer-friendly format.
        /// </summary>
        /// <param name="character">The character that will speak the inserted line.</param>
        /// <param name="newParagraph">Whether the dialogue should start on a new paragraph.</param>
        /// <returns>A dialogue placeholder ready to be inserted into the chapter editor.</returns>
        public static string Create(Character character, bool newParagraph)
        {
            var addNewLine = newParagraph ? Environment.NewLine : string.Empty;
            return $"{addNewLine}{character.Name}: ";
        }

        /// <summary>
        /// Returns legacy structured dialogue tokens found in the supplied text.
        /// </summary>
        /// <param name="txt">The text to inspect.</param>
        /// <returns>A list of legacy structured dialogue snippets.</returns>
        public static List<string> GetInText(string txt)
        {
            return [.. LegacyStructuredDialogueRegex
                .Matches(txt ?? string.Empty)
                .Cast<Match>()
                .Select(match => match.Value)];
        }

        /// <summary>
        /// Returns all dialogues parsed from the supplied text.
        /// </summary>
        /// <param name="txt">The text to inspect.</param>
        /// <returns>A list of parsed dialogue entries.</returns>
        public static List<Dialogue> GetValuesFromString(string txt)
        {
            return GetLegacyStructuredDialogues(txt);
        }

        /// <summary>
        /// Returns dialogues spoken by the provided character names, supporting both the current unified format and older legacy formats.
        /// </summary>
        /// <param name="txt">The text to inspect.</param>
        /// <param name="characters">The character names to match.</param>
        /// <returns>A filtered list of matching dialogues.</returns>
        public static List<Dialogue> GetFromCharactersFromString(string txt, List<string> characters)
        {
            var characterNames = characters?
                .Where(character => !string.IsNullOrWhiteSpace(character))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new List<string>();

            var dialogues = GetLegacyStructuredDialogues(txt);
            dialogues.AddRange(GetUnifiedDialogues(txt, characterNames));
            dialogues.AddRange(GetLegacySimpleDialogues(txt, characterNames));

            return dialogues
                .Where(dialogue => characterNames.Count == 0 || characterNames.Contains(dialogue.Name, StringComparer.CurrentCultureIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Formats dialogues into plain text for export.
        /// </summary>
        /// <param name="dialogues">The dialogues to format.</param>
        /// <returns>A formatted plain text string.</returns>
        public static string FormatDialoguesToString(List<Dialogue> dialogues)
        {
            string txt = string.Empty;

            foreach (var dialogue in dialogues ?? new List<Dialogue>())
            {
                txt += $"{dialogue.Name.ToUpperInvariant()}: {dialogue.Text}{Environment.NewLine}";
            }

            return txt;
        }

        private static List<Dialogue> GetLegacyStructuredDialogues(string txt)
        {
            return LegacyStructuredDialogueRegex
                .Matches(txt ?? string.Empty)
                .Cast<Match>()
                .Select(match => new Dialogue
                {
                    Name = match.Groups["name"].Value.Trim(),
                    Text = match.Groups["text"].Value.Trim(),
                })
                .Where(dialogue => !string.IsNullOrWhiteSpace(dialogue.Name))
                .ToList();
        }

        private static List<Dialogue> GetUnifiedDialogues(string txt, IReadOnlyCollection<string> characterNames)
        {
            var dialogues = new List<Dialogue>();

            if (string.IsNullOrWhiteSpace(txt) || characterNames is null || characterNames.Count == 0)
                return dialogues;

            var normalizedText = (txt ?? string.Empty).Replace("\r\n", "\n");
            var lines = normalizedText.Split('\n');
            var orderedNames = characterNames
                .OrderByDescending(name => name.Length)
                .ToList();

            for (int i = 0; i < lines.Length; i++)
            {
                var currentLine = lines[i].Trim();

                if (!TryGetUnifiedDialogueHeader(currentLine, orderedNames, out var characterName, out var inlineDialogue))
                    continue;

                var buffer = new List<string>();

                if (!string.IsNullOrWhiteSpace(inlineDialogue))
                    buffer.Add(inlineDialogue);

                var j = i + 1;
                while (j < lines.Length)
                {
                    var nextLine = lines[j].Trim();

                    if (string.IsNullOrWhiteSpace(nextLine))
                    {
                        if (buffer.Count > 0)
                            break;

                        j++;
                        continue;
                    }

                    if (TryGetUnifiedDialogueHeader(nextLine, orderedNames, out _, out _))
                        break;

                    buffer.Add(nextLine);
                    j++;
                }

                if (buffer.Count > 0)
                {
                    dialogues.Add(new Dialogue
                    {
                        Name = characterName,
                        Text = string.Join(" ", buffer).Trim(),
                    });

                    i = j - 1;
                }
            }

            return dialogues;
        }

        private static List<Dialogue> GetLegacySimpleDialogues(string txt, IReadOnlyCollection<string> characterNames)
        {
            var dialogues = new List<Dialogue>();

            if (string.IsNullOrWhiteSpace(txt) || characterNames is null || characterNames.Count == 0)
                return dialogues;

            var nameLookup = characterNames.ToDictionary(name => name.ToUpperInvariant(), name => name, StringComparer.OrdinalIgnoreCase);
            var lines = (txt ?? string.Empty).Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var currentLine = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(currentLine) || currentLine.StartsWith("{name=", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!nameLookup.TryGetValue(currentLine.ToUpperInvariant(), out var characterName))
                    continue;

                var buffer = new List<string>();
                var j = i + 1;

                while (j < lines.Length)
                {
                    var dialogueLine = lines[j].Trim();

                    if (string.IsNullOrWhiteSpace(dialogueLine))
                    {
                        if (buffer.Count > 0)
                            break;

                        j++;
                        continue;
                    }

                    if (dialogueLine.StartsWith("{name=", StringComparison.OrdinalIgnoreCase) || nameLookup.ContainsKey(dialogueLine.ToUpperInvariant()))
                        break;

                    buffer.Add(dialogueLine);
                    j++;
                }

                if (buffer.Count > 0)
                {
                    dialogues.Add(new Dialogue
                    {
                        Name = characterName,
                        Text = string.Join(" ", buffer),
                    });

                    i = j - 1;
                }
            }

            return dialogues;
        }

        private static bool TryGetUnifiedDialogueHeader(string line, IReadOnlyList<string> characterNames, out string characterName, out string inlineDialogue)
        {
            characterName = null;
            inlineDialogue = null;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            foreach (var knownCharacterName in characterNames)
            {
                if (!line.StartsWith($"{knownCharacterName}:", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                characterName = knownCharacterName;
                inlineDialogue = line.Substring(knownCharacterName.Length + 1).Trim();
                return true;
            }

            return false;
        }
    }
}
