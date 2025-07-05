using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using WinBlur.App.Model;
using Windows.UI;

namespace WinBlur.App.ViewModel
{
    public class ArticleTheme : INotifyPropertyChanged
    {
        private static readonly ArticleTheme _instance = new ArticleTheme();
        public static ArticleTheme Instance => _instance;

        private ArticleTheme()
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
                NotifyPropertyChanged(nameof(ContentForegroundBrush));
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
                        return (App.Current.Resources["ArticleContentBackgroundBrush"] as SolidColorBrush).Color;
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

        private SolidColorBrush sepiaForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 42, 20, 9)); // #2A1409
        public SolidColorBrush ContentForegroundBrush
        {
            get
            {
                switch (ReadingTheme)
                {
                    case ReadingThemeMode.UseWindowsTheme:
                        return App.Current.Resources["ArticleContentForegroundBrush"] as SolidColorBrush;
                    case ReadingThemeMode.Light:
                        return ((ResourceDictionary)App.Current.Resources.ThemeDictionaries["Light"])["TextFillColorPrimaryBrush"] as SolidColorBrush;
                    case ReadingThemeMode.Dark:
                    case ReadingThemeMode.Black:
                        return ((ResourceDictionary)App.Current.Resources.ThemeDictionaries["Default"])["TextFillColorPrimaryBrush"] as SolidColorBrush;
                    case ReadingThemeMode.Sepia:
                        return sepiaForegroundBrush;
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
            NotifyPropertyChanged(nameof(ContentForegroundBrush));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
