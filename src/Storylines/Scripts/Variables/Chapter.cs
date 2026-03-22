using System.ComponentModel;
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
