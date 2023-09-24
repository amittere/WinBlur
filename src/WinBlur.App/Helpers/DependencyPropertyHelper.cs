using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinBlur.App.Helpers
{
    class DependencyPropertyHelper
    {
        // "HtmlString" attached property for a WebView
        public static readonly DependencyProperty HtmlStringProperty =
           DependencyProperty.RegisterAttached("HtmlString", typeof(string), typeof(DependencyPropertyHelper), new PropertyMetadata("", OnHtmlStringChanged));

        // Getter and Setter
        public static string GetHtmlString(DependencyObject obj) { return (string)obj.GetValue(HtmlStringProperty); }
        public static void SetHtmlString(DependencyObject obj, string value) { obj.SetValue(HtmlStringProperty, value); }

        // Handler for property changes in the DataContext : set the WebView
        private static void OnHtmlStringChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is WebView2 wv && wv.IsLoaded && wv.CoreWebView2 != null)
            {
                wv.NavigateToString(string.Concat(GetHtmlStyleHeader(obj), (string)e.NewValue));
            }
        }

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

        public static readonly DependencyProperty HtmlContentScrollbarColorProperty =
            DependencyProperty.RegisterAttached("HtmlContentScrollbarColor", typeof(SolidColorBrush), typeof(DependencyPropertyHelper), new PropertyMetadata(null));

        public static SolidColorBrush GetHtmlContentScrollbarColor(DependencyObject obj) { return (SolidColorBrush)obj.GetValue(HtmlContentScrollbarColorProperty); }
        public static void SetHtmlContentScrollbarColor(DependencyObject obj, SolidColorBrush value) { obj.SetValue(HtmlContentScrollbarColorProperty, value); }

        private static string htmlStyleHeader;
        private static string htmlBackgroundColor;
        private static string htmlForegroundColor;
        private static string htmlLinkColor;
        private static string htmlScrollbarColor;
        private static string GetHtmlStyleHeader(DependencyObject obj)
        {
            string bgColor = GetHtmlContentBackground(obj).Color.ToString();
            string fgColor = GetHtmlContentForeground(obj).Color.ToString();
            string linkColor = GetHtmlContentLinkColor(obj).Color.ToString();
            string scrollbarColor = GetHtmlContentScrollbarColor(obj).Color.ToString();

            if (bgColor != htmlBackgroundColor ||
                fgColor != htmlForegroundColor ||
                linkColor != htmlLinkColor ||
                scrollbarColor != htmlScrollbarColor)
            {
                // Style changed - update strings and return
                htmlBackgroundColor = bgColor;
                htmlForegroundColor = fgColor;
                htmlLinkColor = linkColor;
                htmlScrollbarColor = scrollbarColor;
                htmlStyleHeader = string.Format(@"
                    <head>
                        <meta name=""viewport"" content=""initial-scale=1 minimum-scale=1"" />
                        <style type=""text/css"">
                            html {{
                                background-color: #{0};
                                color: #{1};
                                font-family: ""Segoe UI Variable"", ""Segoe UI"", sans-serif;
                                line-height: 175%;
                                max-width: 700px;
                                margin: auto;
                                padding: 0px 11px;
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
                            .winblur-caption {{
                                font-size: 12px;
                                line-height: 18px;
                                margin-bottom: 24px;
                            }}
                            ::-webkit-scrollbar {{
                                width: 10px;
                            }}
                            ::-webkit-scrollbar-track {{
                                background: #{0};
                            }}
                            ::-webkit-scrollbar-thumb {{
                                background: #{3};
                                border-radius: 10px;
                                border: 4px solid #{0};
                            }}
                        </style>
                    </head>",
                    bgColor.Substring(3),
                    fgColor.Substring(3),
                    linkColor.Substring(3),
                    scrollbarColor.Substring(3));
            }
            return htmlStyleHeader;
        }
    }
}