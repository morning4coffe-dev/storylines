using Storylines.Pages;
using Storylines.Scripts.Functions;
using Storylines.Scripts.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public static ObservableCollection<Chapter> chapters = new ObservableCollection<Chapter>();
        //public static ObservableCollection<Chapter> chapters 
        //{
        //    get 
        //    {              
        //        MainPage.ChapterList.CheckForEmptyList();
        //        return _chapters;
        //    }
        //    set 
        //    {
        //        _chapters = value;
        //        MainPage.ChapterList.CheckForEmptyList();
        //    }
        //}

        private static readonly Components.ChaptersList ChapterList = MainPage.ChapterList;

        public event PropertyChangedEventHandler PropertyChanged;

        public static void Add(string name)
        {
            var ch = AddExisting(name, Guid.NewGuid().ToString(), string.Empty);
            TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Added, ch, 0);
        }

        public static void AddFromCreator(int i, string txt)
        {
            string chapterName = SettingsValues.chapterName;
            if (chapterName.Contains("{number}"))
                chapterName = chapterName.Replace("{number}", i.ToString());
            else
            if (chapterName.Contains("{cislo}"))
                chapterName = chapterName.Replace("{cislo}", i.ToString());

            Add($"{chapterName}: {txt}");
        }

        public static Chapter AddExisting(string name, string token, string text)
        {
            var ch = new Chapter() { name = name, token = token, text = text };
            chapters.Add(ch);
            return ch;
        }

        public static Chapter InsertExisting(string name, string token, string text, int position)
        {
            var ch = new Chapter() { name = name, token = token, text = text };
            chapters.Insert(position, ch);
            return ch;
        }

        public static void Rename(string token, string newName)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i].token == token)
                {  
                    TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Name, chapters[i], 0);

                    chapters[i].name = newName;
                    (ChapterList.listView.Items[i] as Chapter).name = newName;
                }
            }
        }

        public static void Remove(string token)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i].token == token)
                {
                    TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Removed, chapters[i], chapters.IndexOf(chapters[i]));

                    chapters.RemoveAt(i);
                }
            }
        }

        public static Chapter Copy(string token)
        {
                return (Chapter)Find(token).MemberwiseClone();
        }

        public static Chapter Find(string token)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i].token == token)
                    return chapters[i];
            }
            return null;
        }

        public static int FindID(string token)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i].token == token)
                    return i;
            }
            return 0;
        }

        public static void Reorder(string token, int newPosition, int lastPosition)
        {
            Chapter chapter = Find(token);
            TimeTravelChapter.SomethingChanged(TimeTravelChapter.Changed.Reordered, chapter, lastPosition);

            _ = chapters.Remove(chapter);
            chapters.Insert(newPosition, chapter);
        }

        public void SetToken(string token)
        { 
            this.token = token;
        }

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
