using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Storylines.Models.Dialogue
{
    public partial class DialogueChoice : ObservableObject
    {
        [ObservableProperty]
        private string id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string sourceNodeId = string.Empty;

        [ObservableProperty]
        private string targetNodeId = string.Empty;

        [ObservableProperty]
        private string choiceText = string.Empty;

        [ObservableProperty]
        private string conditions = string.Empty;

        [ObservableProperty]
        private string effects = string.Empty;
    }
}
