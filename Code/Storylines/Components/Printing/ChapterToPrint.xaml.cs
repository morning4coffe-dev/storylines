using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Storylines.Components.Printing
{
    public sealed partial class ChapterToPrint : Page
    {
        public ChapterToPrint()
        {
            this.InitializeComponent();
        }

        public static ChapterToPrint NewPage(string header, string text)
        {
            var pg = new ChapterToPrint();

            var headerTxt = new TextBox()
            {
                Text = header,
                FontSize = 100,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            RichTextBlock txt = new RichTextBlock();

            Run run = new Run
            {
                Text = text,
                Foreground = new SolidColorBrush(Colors.Black)
            };

            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(run);
            txt.Blocks.Add(paragraph);

            pg.printPageStack.Children.Add(headerTxt);
            pg.printPageStack.Children.Add(txt);

            return pg;
        }
        //NewChapterOnPage
    }
}
