using System;
using System.Collections.Generic;

namespace Storylines.Models.Dialogue
{
    public class DialogueGraph
    {
        public List<DialogueNode> Nodes { get; set; } = new List<DialogueNode>();
        public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
    }
}
