using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using WinBlur.App.Model;
using Windows.UI;

namespace WinBlur.App.ViewModel
{
    public class ArticleThemeViewModel : INotifyPropertyChanged
    {
        private static readonly ArticleThemeViewModel _instance = new ArticleThemeViewModel();
        public static ArticleThemeViewModel Instance => _instance;

        private ArticleThemeViewModel()
        {
            contentFontFamily = App.Settings.ReadingFont;
            contentFontWeight = App.Settings.ReadingFontWeight;
            contentTextSize = App.Settings.ReadingTextSize;
            contentLineHeight = App.Settings.ReadingLineHeight;
            contentColumnWidth = App.Settings.ReadingColumnWidth;
        }

        private ReadingThemeMode readingTheme;
        public ReadingThemeMode ReadingTheme
        {
            get { return readingTheme; }
            set
            {
                readingTheme = value;
                App.Settings.ReadingTheme = value;
                NotifyPropertyChanged(nameof(ReadingTheme));
                NotifyPropertyChanged(nameof(RequestedTheme));
                NotifyPropertyChanged(nameof(WebViewBackgroundColor));
                NotifyPropertyChanged(nameof(WebViewBackgroundBrush));
                NotifyPropertyChanged(nameof(ContentBackgroundColor));
                NotifyPropertyChanged(nameof(ContentForegroundColor));
            }
        }

        // Returns what requested theme to use for the given reading theme.
        public ElementTheme RequestedTheme
        {
            get
            {
                switch (ReadingTheme)
                {
                    case ReadingThemeMode.UseWindowsTheme:
                        return App.Settings.AppThemeToElementTheme(Application.Current.RequestedTheme);

                    case ReadingThemeMode.Light:
                    case ReadingThemeMode.Sepia:
                        return ElementTheme.Light;

                    case ReadingThemeMode.Dark:
                    case ReadingThemeMode.Black:
                        return ElementTheme.Dark;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(ReadingTheme), ReadingTheme, "Invalid reading theme mode specified.");
                }
            }
        }

        public Color WebViewBackgroundColor
        {
            get
            {
                switch (ReadingTheme)
                {
                    case ReadingThemeMode.UseWindowsTheme:
                        return (Color)(App.Current.Resources["LayerFillColorDefault"]);
                    case ReadingThemeMode.Light:
                        return Color.FromArgb(255, 238, 238, 238); // #EEEEEE
                    case ReadingThemeMode.Sepia:
                        return Color.FromArgb(255, 227, 206, 185); // #e3ceb9
                    case ReadingThemeMode.Dark:
                        return Color.FromArgb(255, 28, 28, 28); // #1C1C1C
                    case ReadingThemeMode.Black:
                        return Colors.Black;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ReadingTheme), ReadingTheme, "Invalid reading theme mode specified.");
                }
            }
        }

        public SolidColorBrush WebViewBackgroundBrush
        {
            get { return new SolidColorBrush(WebViewBackgroundColor); }
        }

        public Color ContentBackgroundColor
        {
            get
            {
                switch (ReadingTheme)
                {
                    // System theme has special behavior. WebView background color is drawn as layered mica,
                    // so the content background needs to be transparent to allow that to show through.
                    case ReadingThemeMode.UseWindowsTheme:
                        return Colors.Transparent;
                    default:
                        return WebViewBackgroundColor;
                }
            }
        }

        public Color ContentForegroundColor
        {
            get
            {
                switch (ReadingTheme)
                {
                    case ReadingThemeMode.UseWindowsTheme:
                        return (Color)(App.Current.Resources["TextFillColorPrimary"]);
                    case ReadingThemeMode.Light:
                        return (Color)((ResourceDictionary)App.Current.Resources.ThemeDictionaries["Light"])["TextFillColorPrimary"];
                    case ReadingThemeMode.Dark:
                    case ReadingThemeMode.Black:
                        return (Color)((ResourceDictionary)App.Current.Resources.ThemeDictionaries["Default"])["TextFillColorPrimary"];
                    case ReadingThemeMode.Sepia:
                        return Color.FromArgb(255, 42, 20, 9); // #2A1409
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ReadingTheme), ReadingTheme, "Invalid reading theme mode specified.");
                }
            }
        }

        private string contentFontFamily;
        public string ContentFontFamily
        {
            get { return contentFontFamily; }
            set
            {
                contentFontFamily = value;
                App.Settings.ReadingFont = value;
                NotifyPropertyChanged(nameof(ContentFontFamily));
            }
        }

        private int contentFontWeight;
        public int ContentFontWeight
        {
            get { return contentFontWeight; }
            set
            {
                contentFontWeight = value;
                App.Settings.ReadingFontWeight = value;
                NotifyPropertyChanged(nameof(ContentFontWeight));
            }
        }

        private int contentTextSize;
        public int ContentTextSize
        {
            get { return contentTextSize; }
            set
            {
                contentTextSize = value;
                App.Settings.ReadingTextSize = value;
                NotifyPropertyChanged(nameof(ContentTextSize));
            }
        }

        private double contentLineHeight;
        public double ContentLineHeight
        {
            get { return contentLineHeight; }
            set
            {
                contentLineHeight = value;
                App.Settings.ReadingLineHeight = value;
                NotifyPropertyChanged(nameof(ContentLineHeight));
            }
        }

        private int contentColumnWidth;
        public int ContentColumnWidth
        {
            get { return contentColumnWidth; }
            set
            {
                contentColumnWidth = value;
                App.Settings.ReadingColumnWidth = value;
                NotifyPropertyChanged(nameof(ContentColumnWidth));
            }
        }

        public void OnSystemThemeChanged()
        {
            NotifyPropertyChanged(nameof(ReadingTheme));
            NotifyPropertyChanged(nameof(RequestedTheme));
            NotifyPropertyChanged(nameof(WebViewBackgroundColor));
            NotifyPropertyChanged(nameof(WebViewBackgroundBrush));
            NotifyPropertyChanged(nameof(ContentBackgroundColor));
            NotifyPropertyChanged(nameof(ContentForegroundColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
