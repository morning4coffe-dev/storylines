using System;
using System.Collections.Generic;

namespace Storylines
{
    public class Character
    {
        public string name;
        public string tag;
        public string description;
        //age?, description?, abilities?, gender?, 

        public static Character CreateNew(string name, string description)
        {
            Character ch = new Character() { name = name, tag = Guid.NewGuid().ToString(), description = description };
            Characters.characters.Add(ch);
            return ch;
        }

        public static void Change(int characterNum, string newName, string newDescription)
        {
            Characters.characters[characterNum].name = newName;
            Characters.characters[characterNum].description = newDescription;
        }

        public static void Remove(string tag)
        {
            for (int i = 0; i < Characters.characters.Count; i++)
            {
                if (Characters.characters[i].tag == tag)
                {
                    Characters.characters.Remove(Characters.characters[i]);
                    MainPage.mainPage.SomethingChanged();
                }
            }
        }
    }

    public class Characters
    {
        public static List<Character> characters = new List<Character>();
    }
}
