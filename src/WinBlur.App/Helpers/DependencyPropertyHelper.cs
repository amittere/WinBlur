using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using WinBlur.App.ViewModel;
using Windows.UI;

namespace WinBlur.App.Helpers
{
    class DependencyPropertyHelper
    {
        public static readonly DependencyProperty HtmlContentProperty =
           DependencyProperty.RegisterAttached("HtmlContent", typeof(string), typeof(DependencyPropertyHelper), new PropertyMetadata("", OnHtmlContentChanged));

        public static string GetHtmlContent(DependencyObject obj) { return (string)obj.GetValue(HtmlContentProperty); }
        public static void SetHtmlContent(DependencyObject obj, string value) { obj.SetValue(HtmlContentProperty, value); }

        private static void OnHtmlContentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ReloadContent(obj, (string)e.NewValue);
        }

        public static readonly DependencyProperty HtmlContentHeaderProperty =
            DependencyProperty.RegisterAttached("HtmlContentHeader", typeof(string), typeof(DependencyPropertyHelper), new PropertyMetadata(""));

        public static string GetHtmlContentHeader(DependencyObject obj) { return (string)obj.GetValue(HtmlContentHeaderProperty); }
        public static void SetHtmlContentHeader(DependencyObject obj, string value) { obj.SetValue(HtmlContentHeaderProperty, value); }

        public static readonly DependencyProperty HtmlContentForegroundProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentForeground",
                typeof(Color),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(null, OnHtmlThemePropertyChanged));

        public static Color GetHtmlContentForeground(DependencyObject obj) { return (Color)obj.GetValue(HtmlContentForegroundProperty); }
        public static void SetHtmlContentForeground(DependencyObject obj, Color value) { obj.SetValue(HtmlContentForegroundProperty, value); }

        public static readonly DependencyProperty HtmlContentBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentBackground",
                typeof(Color),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(null, OnHtmlThemePropertyChanged));

        public static Color GetHtmlContentBackground(DependencyObject obj) { return (Color)obj.GetValue(HtmlContentBackgroundProperty); }
        public static void SetHtmlContentBackground(DependencyObject obj, Color value) { obj.SetValue(HtmlContentBackgroundProperty, value); }

        public static readonly DependencyProperty HtmlContentLinkColorProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentLinkColor",
                typeof(SolidColorBrush),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(null, OnHtmlThemePropertyChanged));

        public static SolidColorBrush GetHtmlContentLinkColor(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentLinkColorProperty); }
        public static void SetHtmlContentLinkColor(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentLinkColorProperty, value); }

        public static readonly DependencyProperty HtmlContentScrollbarBackgroundColorProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentScrollbarBackgroundColor",
                typeof(Color),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(null, OnHtmlThemePropertyChanged));

        public static Color GetHtmlContentScrollbarBackgroundColor(DependencyObject obj) { return (Color)obj.GetValue(HtmlContentScrollbarBackgroundColorProperty); }
        public static void SetHtmlContentScrollbarBackgroundColor(DependencyObject obj, Color value) { obj.SetValue(HtmlContentScrollbarBackgroundColorProperty, value); }

        public static readonly DependencyProperty HtmlContentScrollbarColorProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentScrollbarColor",
                typeof(Color),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(null, OnHtmlThemePropertyChanged));

        public static Color GetHtmlContentScrollbarColor(DependencyObject obj) { return (Color)obj.GetValue(HtmlContentScrollbarColorProperty); }
        public static void SetHtmlContentScrollbarColor(DependencyObject obj, Color value) { obj.SetValue(HtmlContentScrollbarColorProperty, value); }

        public static readonly DependencyProperty HtmlContentFontFamilyProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentFontFamily",
                typeof(string),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(App.Settings.ReadingFont, OnHtmlThemePropertyChanged));

        public static string GetHtmlContentFontFamily(DependencyObject obj) { return (string)obj.GetValue(HtmlContentFontFamilyProperty); }
        public static void SetHtmlContentFontFamily(DependencyObject obj, string value) { obj.SetValue(HtmlContentFontFamilyProperty, value); }

        public static readonly DependencyProperty HtmlContentFontWeightProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentFontWeight",
                typeof(int),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(ReadingFontViewModel.DefaultFontWeight, OnHtmlThemePropertyChanged));

        public static int GetHtmlContentFontWeight(DependencyObject obj) { return (int)obj.GetValue(HtmlContentFontWeightProperty); }
        public static void SetHtmlContentFontWeight(DependencyObject obj, int value) { obj.SetValue(HtmlContentFontWeightProperty, value); }

        public static readonly DependencyProperty HtmlContentTextSizeProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentTextSize",
                typeof(int),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(App.Settings.ReadingTextSize, OnHtmlThemePropertyChanged));

        public static int GetHtmlContentTextSize(DependencyObject obj) { return (int)obj.GetValue(HtmlContentTextSizeProperty); }
        public static void SetHtmlContentTextSize(DependencyObject obj, int value) { obj.SetValue(HtmlContentTextSizeProperty, value); }

        public static readonly DependencyProperty HtmlContentLineHeightProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentLineHeight",
                typeof(double),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(App.Settings.ReadingLineHeight, OnHtmlThemePropertyChanged));

        public static double GetHtmlContentLineHeight(DependencyObject obj) { return (double)obj.GetValue(HtmlContentLineHeightProperty); }
        public static void SetHtmlContentLineHeight(DependencyObject obj, double value) { obj.SetValue(HtmlContentLineHeightProperty, value); }

        public static readonly DependencyProperty HtmlContentColumnWidthProperty =
            DependencyProperty.RegisterAttached(
                "HtmlContentColumnWidth",
                typeof(int),
                typeof(DependencyPropertyHelper),
                new PropertyMetadata(App.Settings.ReadingColumnWidth, OnHtmlThemePropertyChanged));

        public static int GetHtmlContentColumnWidth(DependencyObject obj) { return (int)obj.GetValue(HtmlContentColumnWidthProperty); }
        public static void SetHtmlContentColumnWidth(DependencyObject obj, int value) { obj.SetValue(HtmlContentColumnWidthProperty, value); }

        // Handler for all related property changes
        private static void OnHtmlThemePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            // Work around a platform issue where sometimes we get a DataContextChanged event
            // sometime after loading an article, causing bindings to get re-evaluated even though
            // nothing changed. Only reload the content if the value actually changed and we have actual
            // content to show.
            string content = GetHtmlContent(obj);
            if (e.NewValue != e.OldValue && !string.IsNullOrEmpty(content))
            {
                ReloadContent(obj, content);
            }
        }

        private static string htmlStyleHeader;
        private static string htmlForegroundColor;
        private static string htmlBackgroundColor;
        private static string htmlLinkColor;
        private static string htmlScrollbarBackgroundColor;
        private static string htmlScrollbarColor;
        private static string htmlFontFamily;
        private static int htmlFontWeight;
        private static int htmlTextSize;
        private static double htmlLineHeight;
        private static int htmlColumnWidth;

        private static void ReloadContent(DependencyObject obj, string content)
        {
            if (obj is WebView2 wv && wv.IsLoaded && wv.CoreWebView2 != null)
            {
                string style = GetHtmlStyleHeader(obj);
                string header = GetHtmlContentHeader(obj);
                try
                {
                    if (App.TestModeHelper.TestMode)
                    {
                        // Dump stack trace for debug purposes
                        wv.NavigateToString(string.Concat(style, $"<div>{System.Environment.StackTrace}</div>", content));
                    }
                    else
                    {
                        wv.NavigateToString(string.Concat(style, content));
                    }
                }
                catch (Exception)
                {
                    wv.NavigateToString(string.Concat(style, header, "<div>Failed to load story. Try a different reading mode.</div>"));
                }
            }
        }

        private static string articleTextViewScript = ReadScriptFile("View\\ArticleTextViewScript.js");
        private static string ReadScriptFile(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return string.Empty;
        }

        private static string GetHtmlStyleHeader(DependencyObject obj)
        {
            string fgColor = GetHtmlContentForeground(obj).ToString();
            string backgroundColor = GetHtmlContentBackground(obj).ToString();
            string linkColor = GetHtmlContentLinkColor(obj).Color.ToString();
            string scrollbarBackgroundColor = GetHtmlContentScrollbarBackgroundColor(obj).ToString();
            string scrollbarColor = GetHtmlContentScrollbarColor(obj).ToString();
            string fontFamily = string.Format("'{0}', 'Segoe UI Variable Text', 'Segoe UI', sans-serif", GetHtmlContentFontFamily(obj));
            int fontWeight = GetHtmlContentFontWeight(obj);
            int textSize = GetHtmlContentTextSize(obj);
            double lineHeight = GetHtmlContentLineHeight(obj);
            int columnWidth = GetHtmlContentColumnWidth(obj);

            if (fgColor != htmlForegroundColor ||
                backgroundColor != htmlBackgroundColor ||
                linkColor != htmlLinkColor ||
                scrollbarBackgroundColor != htmlScrollbarBackgroundColor ||
                scrollbarColor != htmlScrollbarColor ||
                fontFamily != htmlFontFamily ||
                fontWeight != htmlFontWeight ||
                textSize != htmlTextSize ||
                lineHeight != htmlLineHeight ||
                columnWidth != htmlColumnWidth)
            {
                // Style changed - update strings and return
                htmlForegroundColor = fgColor;
                htmlBackgroundColor = backgroundColor;
                htmlLinkColor = linkColor;
                htmlScrollbarBackgroundColor = scrollbarBackgroundColor;
                htmlScrollbarColor = scrollbarColor;
                htmlFontFamily = fontFamily;
                htmlFontWeight = fontWeight;
                htmlTextSize = textSize;
                htmlLineHeight = lineHeight;
                htmlColumnWidth = columnWidth;

                string fgColorNoAlpha = htmlForegroundColor.Substring(3); // Remove alpha channel
                string backgroundColorNoAlpha = htmlBackgroundColor.Substring(3);
                string bgColorOrTransparent = (GetHtmlContentBackground(obj) == Colors.Transparent) ? "transparent" : $"#{backgroundColorNoAlpha}";
                string linkColorNoAlpha = htmlLinkColor.Substring(3);
                string scrollbarBackgroundColorNoAlpha = htmlScrollbarBackgroundColor.Substring(3);
                string scrollbarColorNoAlpha = htmlScrollbarColor.Substring(3);
                float textSizeEm = textSize / 16f; // Convert text size from px to em to easily support relative sizing

                htmlStyleHeader = $@"
                    <head>
                        <meta name=""viewport"" content=""initial-scale=1 minimum-scale=1"" />
                        <style type=""text/css"">
                            html {{
                                background-color: {bgColorOrTransparent};
                                color: #{fgColorNoAlpha};
                                font-family: {htmlFontFamily};
                                font-size: {textSizeEm}em;
                                font-weight: {htmlFontWeight};
                                line-height: {htmlLineHeight};
                                max-width: {htmlColumnWidth}px;
                                margin: auto;
                                padding: 0px 30px;
                            }}
                            html * {{
                                max-width: 100%;
                                width: auto;
                                height: auto;
                            }}
                            a {{
                                color: #{linkColorNoAlpha};
                                text-decoration: none;
                            }}
                            a:hover {{
                                text-decoration: underline;
                            }}
                            .winblur-title {{
                                font-size: 125%;
                                font-weight: 600;
                                line-height: 1.3;
                                margin-top: 0px;
                                margin-bottom: 0.4em;
                            }}
                            .winblur-title-link {{
                                color: #{fgColorNoAlpha};
                            }}
                            .winblur-caption {{
                                font-size: 75%;
                                line-height: 1.5;
                                margin-bottom: 24px;
                            }}
                            ::-webkit-scrollbar {{
                                width: 8px;
                                height: 8px;
                            }}
                            ::-webkit-scrollbar-track {{
                                background: #{scrollbarBackgroundColorNoAlpha};
                            }}
                            ::-webkit-scrollbar-thumb {{
                                background: #{scrollbarColorNoAlpha};
                                border-radius: 10px;
                                border: 4px solid #{scrollbarColorNoAlpha};
                            }}
                        </style>
                        <script>{articleTextViewScript}</script>
                    </head>";
            }
            return htmlStyleHeader;
        }
    }
}