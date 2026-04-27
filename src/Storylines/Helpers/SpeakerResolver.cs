using Storylines.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Helpers
{
    public static class SpeakerResolver
    {
        public static Character ResolveByToken(string characterToken, IEnumerable<Character> characters)
        {
            if (string.IsNullOrWhiteSpace(characterToken) || characters == null)
                return null;

            return characters.FirstOrDefault(c => c.Token == characterToken);
        }

        public static Character ResolveByName(string speaker, IEnumerable<Character> characters)
        {
            if (string.IsNullOrWhiteSpace(speaker) || characters == null)
                return null;

            return characters.FirstOrDefault(c =>
                string.Equals(c.Name, speaker, StringComparison.CurrentCultureIgnoreCase));
        }

        public static Character Resolve(BranchingDialogueNodeData node, IEnumerable<Character> characters)
        {
            if (node == null || characters == null)
                return null;

            if (!string.IsNullOrWhiteSpace(node.CharacterToken))
            {
                var byToken = ResolveByToken(node.CharacterToken, characters);
                if (byToken != null)
                    return byToken;
            }

            return ResolveByName(node.Speaker, characters);
        }

        public static string ResolveToken(string speaker, IEnumerable<Character> characters)
        {
            var character = ResolveByName(speaker, characters);
            return character?.Token;
        }

        public static HashSet<string> GetKnownSpeakers(IEnumerable<Character> characters)
        {
            if (characters == null)
                return new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            return new HashSet<string>(
                characters
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .Select(c => c.Name),
                StringComparer.CurrentCultureIgnoreCase);
        }

        public static int CountNodesForCharacter(string characterToken, string characterName,
            IEnumerable<BranchingDialogueGraphData> graphs)
        {
            if (graphs == null)
                return 0;

            int count = 0;
            foreach (var graph in graphs)
            {
                if (graph?.Nodes == null)
                    continue;

                foreach (var node in graph.Nodes)
                {
                    if (!string.IsNullOrWhiteSpace(characterToken) &&
                        string.Equals(node.CharacterToken, characterToken, StringComparison.Ordinal))
                    {
                        count++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(characterName) &&
                        string.Equals(node.Speaker, characterName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
