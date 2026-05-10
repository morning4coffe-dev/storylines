using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Storylines.Models.Dialogue
{
    public partial class DialogueNode : ObservableObject
    {
        [ObservableProperty]
        private string id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string speaker = string.Empty;

        [ObservableProperty]
        private string contentRtf = string.Empty;

        [ObservableProperty]
        private string contentPlainText = string.Empty;

        public List<TagOccurrence> Tags { get; set; } = new List<TagOccurrence>();
    }

    public class TagOccurrence
    {
        public string TagName { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
}
