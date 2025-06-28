using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinBlur.App.Model;
using Windows.UI;

namespace WinBlur.App.ViewModel
{
    internal class ReadingThemeViewModel
    {
        public string Label { get; set; }
        public ReadingThemeMode ThemeMode { get; set; }

        // Returns what requested theme to use for the given reading theme.
        public ElementTheme RequestedTheme
        {
            get
            {
                switch (ThemeMode)
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
                        throw new ArgumentOutOfRangeException(nameof(ThemeMode), ThemeMode, "Invalid reading theme mode specified.");
                }
            }
        }

        public static Color GetWebViewBackgroundColorForReadingTheme(ReadingThemeMode themeMode)
        {
            switch (themeMode)
            {
                case ReadingThemeMode.UseWindowsTheme:
                    return (App.Current.Resources["ArticleContentBackgroundBrush_System"] as SolidColorBrush).Color;
                case ReadingThemeMode.Light:
                    return Color.FromArgb(255, 238, 238, 238); // #EEEEEE
                case ReadingThemeMode.Sepia:
                    return Color.FromArgb(255, 255, 245, 220); // #FFF5DC
                case ReadingThemeMode.Dark:
                    return Color.FromArgb(255, 28, 28, 28); // #1C1C1C
                case ReadingThemeMode.Black:
                    return Colors.Black;
                default:
                    throw new ArgumentOutOfRangeException(nameof(themeMode), themeMode, "Invalid reading theme mode specified.");
            }
        }

        public static Color GetContentBackgroundColorForReadingTheme(ReadingThemeMode themeMode)
        {
            switch (themeMode)
            {
                // System theme has special behavior. WebView background color is drawn as layered mica,
                // so the content background needs to be transparent to allow that to show through.
                case ReadingThemeMode.UseWindowsTheme:
                    return Colors.Transparent;
                default:
                    return GetWebViewBackgroundColorForReadingTheme(themeMode);
            }
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
