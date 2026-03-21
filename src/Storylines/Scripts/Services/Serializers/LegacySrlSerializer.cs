using Storylines.Scripts.Services.Interfaces;
using Storylines.Scripts.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Scripts.Services.Serializers
{
    public class LegacySrlSerializer : ISaveSerializer
    {
        private const string PropertyDelimiter = "<Y&⨝m>";
        private const string KeyValueSeparator = ">[Y≇g&<";
        private const string RecordTerminator = "@N*∛$\n";
        private const string RecordTerminatorAlt = "@N*∛$\r\n";

        public string Serialize(ProjectData projectData)
        {
            var lines = new List<string>();

            lines.Add($"version{KeyValueSeparator}{projectData.Version}{RecordTerminator}");
            lines.Add($"lastOpenedChapter{KeyValueSeparator}{projectData.LastOpenedChapter}{RecordTerminator}");
            lines.Add($"name{KeyValueSeparator}{projectData.Name}{RecordTerminator}");

            for (int i = 0; i < projectData.Characters.Count; i++)
            {
                var ch = projectData.Characters[i];
                lines.Add($"character{i}{KeyValueSeparator}{ch.Name}{PropertyDelimiter}{ch.Description}{PropertyDelimiter}{ch.PictureFileName}{RecordTerminator}");
            }

            for (int i = 0; i < projectData.Chapters.Count; i++)
            {
                var ch = projectData.Chapters[i];
                var text = SanitizeText(ch.Text);
                lines.Add($"chapter{i}{KeyValueSeparator}{ch.Name}{PropertyDelimiter}{text}{RecordTerminator}");
            }

            return string.Concat(lines);
        }

        public ProjectData Deserialize(string content)
        {
            var terminator = content.Contains(RecordTerminator) ? RecordTerminator : RecordTerminatorAlt;
            var records = content.Split(new[] { terminator }, StringSplitOptions.RemoveEmptyEntries);

            var dict = new Dictionary<string, string>();
            foreach (var record in records)
            {
                var parts = record.Split(new[] { KeyValueSeparator }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    dict[parts[0]] = parts[1];
            }

            var projectData = new ProjectData
            {
                Format = "legacy-srl",
                Version = dict.ContainsKey("version") ? dict["version"] : "0.0.0.0",
                LastOpenedChapter = dict.ContainsKey("lastOpenedChapter") ? Convert.ToInt32(dict["lastOpenedChapter"]) : 0,
                Name = dict.ContainsKey("name") ? dict["name"] : string.Empty
            };

            foreach (var kvp in dict.Where(k => k.Key.StartsWith("character")))
            {
                var parts = kvp.Value.Split(new[] { PropertyDelimiter }, StringSplitOptions.None);
                projectData.Characters.Add(new CharacterData
                {
                    Name = parts.Length > 0 ? parts[0] : string.Empty,
                    Description = parts.Length > 1 ? parts[1] : string.Empty,
                    PictureFileName = parts.Length > 2 ? parts[2] : string.Empty
                });
            }

            foreach (var kvp in dict.Where(k => k.Key.StartsWith("chapter")))
            {
                var parts = kvp.Value.Split(new[] { PropertyDelimiter }, StringSplitOptions.None);
                projectData.Chapters.Add(new ChapterData
                {
                    Name = parts.Length > 0 ? parts[0] : string.Empty,
                    Text = parts.Length > 1 ? parts[1] : string.Empty,
                    Notes = string.Empty
                });
            }

            return projectData;
        }

        public bool CanDeserialize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return content.Contains(KeyValueSeparator) && (content.Contains(RecordTerminator) || content.Contains(RecordTerminatorAlt));
        }

        private static string SanitizeText(string text)
        {
            if (text == null) return string.Empty;
            return text
                .Replace(PropertyDelimiter, "")
                .Replace(KeyValueSeparator, "")
                .Replace(RecordTerminator, "");
        }
    }
}
