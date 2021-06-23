using System;
using Windows.UI.Xaml.Controls;

namespace Storylines
{
    public class Chapter
    {
        public string name;
        public string text;
        public string token;

        private static readonly Components.ChapterListComponent chapterList = MainPage.chapterList;

        public static void Add(string name)
        {
            AddExisting(name, Guid.NewGuid().ToString(), string.Empty);
            MainPage.mainPage.SomethingChanged();
        }

        public static void AddExisting(string name, string token, string text)
        {
            chapterList.chapters.Add(new Chapter() { name = name, token = token, text = text });

            MainPage.chapterList.chaptersListView.Items.Add(new ListViewItem() { Name = token, Content = name });
        }

        public static void Rename(string token, string newName)
        {
            for (int i = 0; i < chapterList.chapters.Count; i++)
            {
                if (MainPage.chapterList.chapters[i].token == token)
                {
                    chapterList.chapters[i].name = newName;
                    (chapterList.chaptersListView.Items[i] as ListViewItem).Content = newName;

                    MainPage.mainPage.SomethingChanged();
                }
            }
        }

        public static void Remove(string token)
        {
            for (int i = 0; i < chapterList.chapters.Count; i++)
            {
                if (MainPage.chapterList.chapters[i].token == token)
                {
                    chapterList.chapters.RemoveAt(i);
                    chapterList.chaptersListView.Items.RemoveAt(i);

                    MainPage.mainPage.SomethingChanged();
                }
            }
        }

        public static Chapter Find(string token)
        {
            for (int i = 0; i < chapterList.chapters.Count; i++)
            {
                if (MainPage.chapterList.chapters[i].token == token)
                {
                    return MainPage.chapterList.chapters[i];
                }
            }
            return null;
        }

        public static void Reorder(string token, int newPosition)
        {
            for (int i = 0; i < chapterList.chapters.Count; i++)
            {
                if (chapterList.chapters[i].token == token)
                {
                    var item = chapterList.chapters[i];
                    chapterList.chapters.Remove(item);
                    chapterList.chapters.Insert(newPosition, item);

                    MainPage.mainPage.SomethingChanged();
                }
            }
        }
    }
}
