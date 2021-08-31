using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Storylines.Scripts.Variables
{
    public class Dialogue
    {
        public string name { get; set; }
        public string text { get; set; }
        //public string token;

        public static string Create(Character character, bool newParagraph)
        {
            var addNP = newParagraph ? "\n" : "";
            return addNP + "{" + $"name={character.name}; text=\u0022\u0022" + "}";
        }

        public static string CreateSimple(Character character, bool newParagraph)
        {
            var addNP = newParagraph ? "\n" : "";
            return $"{addNP}{character.name.ToUpper()}\n";
        }

        public static List<string> GetInText(string txt)
        {
            var matches = new List<string>();

            foreach (Match match in Regex.Matches(txt, @"\{"))
            {
                matches.Add(txt.Substring(match.Index, txt.IndexOf("}", match.Index) - match.Index + 1));
            }

            return matches;
        }

        public static List<Dialogue> GetValuesFromString(string txt)
        {
            var matches = GetInText(txt);
            List<Dialogue> dialogues = new List<Dialogue>();

            if (matches.Count > 0)
            {
                foreach (string match in matches)//spadne, pokud match = 0
                {
                    var dialogueStrings = match.Split("; ", StringSplitOptions.RemoveEmptyEntries);
                    matches.Remove("}");
                    matches.Remove("\u0022");

                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    foreach (string dictMatch in dialogueStrings)
                    {
                        var spitDict = dictMatch.Split("=", StringSplitOptions.None);
                        spitDict[0] = spitDict[0].Replace("{", string.Empty);
                        spitDict[0] = spitDict[0].Replace("}", string.Empty);

                        dict.Add(spitDict[0], spitDict[1]);
                    }

                    dialogues.Add(new Dialogue() { name = dict["name"], text = dict["text"] });
                }

                return dialogues;
            }

            return null;
        }

        public static List<Dialogue> GetFromCharactersFromString(string txt, List<string> characters)//přepracovat
        {
            var dialogues = GetValuesFromString(txt);
            var dialogues2 = new List<Dialogue>();
            var characterNames = new List<string>();

            foreach (var x in dialogues)
            {
                characterNames.Add(x.name);
            }

            for (int i = 0; i < characterNames.Count; i++)
            {
                if (characters.Contains(characterNames[i]))
                    dialogues2.Add(dialogues[i]);
            }
            return dialogues2;
        }

        public static string FormatDialoguesToString(List<Dialogue> dialogues)
        {
            string txt  = "";
            foreach (var dialogue in dialogues)
            {
                txt += $"{dialogue.name.ToUpper()}: {dialogue.text}\n";
            }
            return txt;
        }
    }
}
