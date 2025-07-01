using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
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
            if (obj is WebView2 wv && wv.IsLoaded && wv.CoreWebView2 != null)
            {
                string style = GetHtmlStyleHeader(obj);
                string header = GetHtmlContentHeader(obj);
                try
                {
                    wv.NavigateToString(string.Concat(style, header, (string)e.NewValue));
                }
                catch (Exception)
                {
                    wv.NavigateToString(string.Concat(style, header, "<div>Failed to load story. Try a different reading mode.</div>"));
                }
            }
        }


        public static readonly DependencyProperty HtmlContentHeaderProperty =
            DependencyProperty.RegisterAttached("HtmlContentHeader", typeof(string), typeof(DependencyPropertyHelper), new PropertyMetadata(""));

        public static string GetHtmlContentHeader(DependencyObject obj) { return (string)obj.GetValue(HtmlContentHeaderProperty); }
        public static void SetHtmlContentHeader(DependencyObject obj, string value) { obj.SetValue(HtmlContentHeaderProperty, value); }

        public static readonly DependencyProperty HtmlContentForegroundProperty =
            DependencyProperty.RegisterAttached("HtmlContentForeground", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentForeground(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentForegroundProperty); }
        public static void SetHtmlContentForeground(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentForegroundProperty, value); }

        public static readonly DependencyProperty HtmlContentLinkColorProperty =
            DependencyProperty.RegisterAttached("HtmlContentLinkColor", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentLinkColor(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentLinkColorProperty); }
        public static void SetHtmlContentLinkColor(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentLinkColorProperty, value); }

        public static readonly DependencyProperty HtmlContentScrollbarBackgroundColorProperty =
            DependencyProperty.RegisterAttached("HtmlContentScrollbarBackgroundColor", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentScrollbarBackgroundColor(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentScrollbarBackgroundColorProperty); }
        public static void SetHtmlContentScrollbarBackgroundColor(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentScrollbarBackgroundColorProperty, value); }

        public static readonly DependencyProperty HtmlContentScrollbarColorProperty =
            DependencyProperty.RegisterAttached("HtmlContentScrollbarColor", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentScrollbarColor(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentScrollbarColorProperty); }
        public static void SetHtmlContentScrollbarColor(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentScrollbarColorProperty, value); }

        public static readonly DependencyProperty HtmlContentTextSizeProperty =
            DependencyProperty.RegisterAttached("HtmlContentTextSize", typeof(int), typeof(DependencyPropertyHelper), new PropertyMetadata(App.Settings.ReadingTextSize));

        public static int GetHtmlContentTextSize(DependencyObject obj) { return (int)obj.GetValue(HtmlContentTextSizeProperty); }
        public static void SetHtmlContentTextSize(DependencyObject obj, int value) { obj.SetValue(HtmlContentTextSizeProperty, value); }

        public static readonly DependencyProperty HtmlContentLineHeightProperty =
            DependencyProperty.RegisterAttached("HtmlContentLineHeight", typeof(double), typeof(DependencyPropertyHelper), new PropertyMetadata(App.Settings.ReadingLineHeight));

        public static double GetHtmlContentLineHeight(DependencyObject obj) { return (double)obj.GetValue(HtmlContentLineHeightProperty); }
        public static void SetHtmlContentLineHeight(DependencyObject obj, double value) { obj.SetValue(HtmlContentLineHeightProperty, value); }

        public static readonly DependencyProperty HtmlContentColumnWidthProperty =
            DependencyProperty.RegisterAttached("HtmlContentColumnWidth", typeof(int), typeof(DependencyPropertyHelper), new PropertyMetadata(App.Settings.ReadingColumnWidth));

        public static int GetHtmlContentColumnWidth(DependencyObject obj) { return (int)obj.GetValue(HtmlContentColumnWidthProperty); }
        public static void SetHtmlContentColumnWidth(DependencyObject obj, int value) { obj.SetValue(HtmlContentColumnWidthProperty, value); }

        private static string htmlStyleHeader;
        private static string htmlForegroundColor;
        private static string htmlLinkColor;
        private static string htmlScrollbarBackgroundColor;
        private static string htmlScrollbarColor;
        private static int htmlTextSize;
        private static double htmlLineHeight;
        private static int htmlColumnWidth;

        private static string GetHtmlStyleHeader(DependencyObject obj)
        {
            string fgColor = GetHtmlContentForeground(obj).Color.ToString();
            string linkColor = GetHtmlContentLinkColor(obj).Color.ToString();
            string scrollbarBackgroundColor = GetHtmlContentScrollbarBackgroundColor(obj).Color.ToString();
            string scrollbarColor = GetHtmlContentScrollbarColor(obj).Color.ToString();
            int textSize = GetHtmlContentTextSize(obj);
            double lineHeight = GetHtmlContentLineHeight(obj);
            int columnWidth = GetHtmlContentColumnWidth(obj);

            if (fgColor != htmlForegroundColor ||
                linkColor != htmlLinkColor ||
                scrollbarBackgroundColor != htmlScrollbarBackgroundColor ||
                scrollbarColor != htmlScrollbarColor ||
                textSize != htmlTextSize ||
                lineHeight != htmlLineHeight ||
                columnWidth != htmlColumnWidth)
            {
                // Style changed - update strings and return
                htmlForegroundColor = fgColor;
                htmlLinkColor = linkColor;
                htmlScrollbarBackgroundColor = scrollbarBackgroundColor;
                htmlScrollbarColor = scrollbarColor;
                htmlTextSize = textSize;
                htmlLineHeight = lineHeight;
                htmlColumnWidth = columnWidth;

                // Convert text size from px to em to easily support relative sizing
                float textSizeEm = textSize / 16f; // Assuming 16px is the base font size

                htmlStyleHeader = string.Format(@"
                    <head>
                        <meta name=""viewport"" content=""initial-scale=1 minimum-scale=1"" />
                        <style type=""text/css"">
                            html {{
                                color: #{0};
                                font-family: ""Segoe UI Variable"", ""Segoe UI"", sans-serif;
                                font-size: {4}em;
                                line-height: {5};
                                max-width: {6}px;
                                margin: auto;
                                padding: 0px 30px;
                            }}
                            html * {{
                                max-width: 100%;
                                width: auto;
                                height: auto;
                            }}
                            a {{
                                color: #{1};
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
                                color: #{0};
                            }}
                            .winblur-caption {{
                                font-size: 75%;
                                line-height: 1.5;
                                margin-bottom: 24px;
                            }}
                            ::-webkit-scrollbar {{
                                width: 10px;
                            }}
                            ::-webkit-scrollbar-track {{
                                background: #{2};
                            }}
                            ::-webkit-scrollbar-thumb {{
                                background: #{3};
                                border-radius: 10px;
                                border: 4px solid #{3};
                            }}
                        </style>
                    </head>",
                    fgColor.Substring(3),
                    linkColor.Substring(3),
                    scrollbarBackgroundColor.Substring(3),
                    scrollbarColor.Substring(3),
                    textSizeEm,
                    lineHeight,
                    columnWidth);
            }
            return htmlStyleHeader;
        }
    }
}