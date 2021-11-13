using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Dokumentaci k šabloně položky Uživatelský ovládací prvek najdete na adrese https://go.microsoft.com/fwlink/?LinkId=234236.

namespace Storylines.Components.CustomControls
{
    public sealed partial class ShortcutLineControl : UserControl
    {
        public ShortcutLineControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
          "Description",
          typeof(string),
          typeof(ShortcutLineControl),
          new PropertyMetadata(null)
        );

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty UseControlProperty = DependencyProperty.Register(
          "UseControl",
          typeof(bool),
          typeof(ShortcutLineControl),
          new PropertyMetadata(null)
        );

        public bool UseControl
        {
            get => (bool)GetValue(UseControlProperty);
            set => SetValue(UseControlProperty, value);
        }

        public static readonly DependencyProperty UseShiftProperty = DependencyProperty.Register(
          "UseShift",
          typeof(bool),
          typeof(ShortcutLineControl),
          new PropertyMetadata(null)
        );

        public bool UseShift
        {
            get => (bool)GetValue(UseShiftProperty);
            set => SetValue(UseShiftProperty, value);
        }

        public static readonly DependencyProperty ShorcutProperty = DependencyProperty.Register(
          "Shorcut",
          typeof(string),
          typeof(ShortcutLineControl),
          new PropertyMetadata(null)
        );

        public string Shortcut
        {
            get => (string)GetValue(ShorcutProperty);
            set => SetValue(ShorcutProperty, value);
        }

        private string ShortcutText
        {
            get => $"{((bool)GetValue(UseControlProperty) ? "Ctrl+" : "") }{((bool)GetValue(UseShiftProperty) ? "Shift+" : "")}{GetValue(ShorcutProperty)}";
        }
    }
}
