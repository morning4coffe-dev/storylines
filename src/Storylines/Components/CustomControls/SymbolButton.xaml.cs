using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Storylines.Components.CustomControls
{
    public sealed partial class SymbolButton : UserControl
    {
        public SymbolButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
          "Text",
          typeof(string),
          typeof(SymbolButton),
          new PropertyMetadata(null)
        );

        public string Text
        {
            get => IsCancel ? ResourceLoader.GetForCurrentView().GetString("cancelText") : (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        //public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        //  "Symbol",
        //  typeof(string),
        //  typeof(SymbolButton),
        //  new PropertyMetadata(null)
        //);

        ////font icon
        //public Symbol Symbol
        //{
        //    get => Glyph == null ? Symbol.Emoji : IsCancel ? Symbol.Cancel : (Symbol)GetValue(SymbolProperty);
        //    set => SetValue(SymbolProperty, value);
        //}

        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
          "Glyph",
          typeof(string),
          typeof(SymbolButton),
          new PropertyMetadata(null)
        );

        public string Glyph
        {
            get => IsCancel ? "" : (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public static readonly DependencyProperty IsCancelProperty = DependencyProperty.Register(
          "IsCancel",
          typeof(bool),
          typeof(SymbolButton),
          new PropertyMetadata(null)
        );

        public bool IsCancel
        {
            get => (bool)GetValue(IsCancelProperty);
            set => SetValue(IsCancelProperty, value);
        }

        public static readonly DependencyProperty IsPrimaryProperty = DependencyProperty.Register(
          "IsPrimary",
          typeof(bool),
          typeof(SymbolButton),
          new PropertyMetadata(null)
        );

        public bool IsPrimary
        {
            set
            {
                SetValue(IsPrimaryProperty, value);
                Style = value ? (Style)Application.Current.Resources["AccentButtonStyle"] : default;
            }
        }

        private Style Style { set; get; }

        public event RoutedEventHandler Click;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
