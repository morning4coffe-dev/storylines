using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Storylines.Models.Dialogue;

namespace Storylines.ViewModels.Dialogue
{
    public partial class DialogueNodeViewModel : ObservableObject
    {
        public DialogueNode Node { get; }

        public DialogueNodeViewModel(DialogueNode node)
        {
            Node = node;
            Choices = new ObservableCollection<DialogueChoiceViewModel>();
        }

        public string Id => Node.Id;

        public string Speaker
        {
            get => Node.Speaker;
            set
            {
                if (Node.Speaker != value)
                {
                    Node.Speaker = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Speaker) ? "Untitled Node" : Speaker;

        public ObservableCollection<DialogueChoiceViewModel> Choices { get; }
    }

    public partial class DialogueChoiceViewModel : ObservableObject
    {
        public DialogueChoice Choice { get; }

        public DialogueChoiceViewModel(DialogueChoice choice)
        {
            Choice = choice;
        }

        public string ChoiceText
        {
            get => Choice.ChoiceText;
            set
            {
                if (Choice.ChoiceText != value)
                {
                    Choice.ChoiceText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetNodeId
        {
            get => Choice.TargetNodeId;
            set
            {
                if (Choice.TargetNodeId != value)
                {
                    Choice.TargetNodeId = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
