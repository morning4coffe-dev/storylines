using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Storylines.Scripts.Services
{
    /// <summary>
    /// Manages the set of user-defined chapter tag presets stored in LocalSettings.
    /// </summary>
    public static class ChapterTagsService
    {
        // Built-in starter presets shown on first use
        private static readonly List<string> DefaultPresets = new List<string>
        {
            "Scene", "Flashback", "Dialogue-heavy", "Action", "Emotional",
            "Foreshadowing", "Revelation", "Transition", "Prologue", "Epilogue"
        };

        public static List<string> GetPresets()
        {
            try
            {
                var raw = ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterTagPresets]?.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                    return JsonConvert.DeserializeObject<List<string>>(raw) ?? new List<string>(DefaultPresets);
            }
            catch { /* corrupt — reset */ }

            // First time: persist defaults so user can edit them later
            SavePresets(new List<string>(DefaultPresets));
            return new List<string>(DefaultPresets);
        }

        public static void AddPreset(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var presets = GetPresets();
            if (!presets.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            {
                presets.Add(tag.Trim());
                SavePresets(presets);
            }
        }

        public static void RemovePreset(string tag)
        {
            var presets = GetPresets();
            presets.RemoveAll(p => string.Equals(p, tag, StringComparison.CurrentCultureIgnoreCase));
            SavePresets(presets);
        }

        public static void SavePresets(List<string> presets)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[SettingsValueStrings.ChapterTagPresets] =
                    JsonConvert.SerializeObject(presets ?? new List<string>());
            }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// Returns all unique tags currently used across the given chapters
        /// merged with the stored presets — useful for suggestion lists.
        /// </summary>
        public static List<string> GetAllSuggestions(IEnumerable<Variables.Chapter> chapters)
        {
            var presets = GetPresets();
            var usedTags = chapters?
                .SelectMany(c => c.Tags ?? new System.Collections.Generic.List<string>())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new List<string>();

            return presets
                .Concat(usedTags)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(t => t)
                .ToList();
        }
    }
}
