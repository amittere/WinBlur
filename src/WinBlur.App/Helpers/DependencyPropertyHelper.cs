using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

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

        public static readonly DependencyProperty HtmlContentBackgroundProperty =
            DependencyProperty.RegisterAttached("HtmlContentBackground", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentBackground(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentBackgroundProperty); }
        public static void SetHtmlContentBackground(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentBackgroundProperty, value); }

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

        private static string htmlStyleHeader;
        private static string htmlBackgroundColor;
        private static string htmlForegroundColor;
        private static string htmlLinkColor;
        private static string htmlScrollbarBackgroundColor;
        private static string htmlScrollbarColor;
        private static string GetHtmlStyleHeader(DependencyObject obj)
        {
            string bgColor = GetHtmlContentBackground(obj).Color.ToString();
            string fgColor = GetHtmlContentForeground(obj).Color.ToString();
            string linkColor = GetHtmlContentLinkColor(obj).Color.ToString();
            string scrollbarBackgroundColor = GetHtmlContentScrollbarBackgroundColor(obj).Color.ToString();
            string scrollbarColor = GetHtmlContentScrollbarColor(obj).Color.ToString();

            if (bgColor != htmlBackgroundColor ||
                fgColor != htmlForegroundColor ||
                linkColor != htmlLinkColor ||
                scrollbarBackgroundColor != htmlScrollbarBackgroundColor ||
                scrollbarColor != htmlScrollbarColor)
            {
                // Style changed - update strings and return
                htmlBackgroundColor = bgColor;
                htmlForegroundColor = fgColor;
                htmlLinkColor = linkColor;
                htmlScrollbarBackgroundColor = scrollbarBackgroundColor;
                htmlScrollbarColor = scrollbarColor;
                htmlStyleHeader = string.Format(@"
                    <head>
                        <meta name=""viewport"" content=""initial-scale=1 minimum-scale=1"" />
                        <style type=""text/css"">
                            html {{
                                color: #{1};
                                font-family: ""Segoe UI Variable"", ""Segoe UI"", sans-serif;
                                line-height: 175%;
                                max-width: 700px;
                                margin: auto;
                                padding: 0px 30px;
                            }}
                            html * {{
                                max-width: 100%;
                                width: auto;
                                height: auto;
                            }}
                            a {{
                                color: #{2};
                                text-decoration: none;
                            }}
                            a:hover {{
                                text-decoration: underline;
                            }}
                            .winblur-title {{
                                font-size: 20px;
                                font-weight: 600;
                                line-height: 26px;
                                margin-top: 0px;
                                margin-bottom: 8px;
                            }}
                            .winblur-title-link {{
                                color: #{1};
                            }}
                            .winblur-caption {{
                                font-size: 12px;
                                line-height: 18px;
                                margin-bottom: 24px;
                            }}
                            ::-webkit-scrollbar {{
                                width: 10px;
                            }}
                            ::-webkit-scrollbar-track {{
                                background: #{3};
                            }}
                            ::-webkit-scrollbar-thumb {{
                                background: #{4};
                                border-radius: 10px;
                                border: 4px solid #{3};
                            }}
                        </style>
                    </head>",
                    bgColor.Substring(3),
                    fgColor.Substring(3),
                    linkColor.Substring(3),
                    scrollbarBackgroundColor.Substring(3),
                    scrollbarColor.Substring(3));
            }
            return htmlStyleHeader;
        }
    }
}