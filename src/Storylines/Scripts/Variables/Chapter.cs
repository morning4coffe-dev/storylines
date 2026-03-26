using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Storylines.Scripts.Variables
{
    public class Chapter : INotifyPropertyChanged
    {
        private string _name;
        public string name { get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
        public string token { get; private set; }
        private string _text;
        public string text
        {
            get { return _text; }
            set
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }
        private string _notes;
        public string notes
        {
            get { return _notes; }
            set
            {
                _notes = value;
                NotifyPropertyChanged();
            }
        }

        private string _synopsis;
        public string synopsis
        {
            get { return _synopsis; }
            set
            {
                _synopsis = value;
                NotifyPropertyChanged();
            }
        }

        private int? _wordCountGoal;
        public int? wordCountGoal
        {
            get { return _wordCountGoal; }
            set
            {
                _wordCountGoal = value;
                NotifyPropertyChanged();
            }
        }

        private List<string> _tags = new List<string>();
        public List<string> tags
        {
            get { return _tags; }
            set
            {
                _tags = value ?? new List<string>();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(tagsText));
            }
        }

        public string tagsText
        {
            get { return _tags == null || _tags.Count == 0 ? string.Empty : string.Join(", ", _tags); }
            set
            {
                tags = (value ?? string.Empty)
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(System.StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetToken(string token)
        {
            this.token = token;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
