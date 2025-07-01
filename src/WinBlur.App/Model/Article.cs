using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI;

namespace WinBlur.App.Model
{
    public class Article : INotifyPropertyChanged
    {
        #region Article Info

        public string ID { get; set; }
        public string Hash { get; set; }
        public Uri ArticleLink { get; set; }
        private bool _isRead;
        public bool IsRead
        {
            get { return _isRead; }
            set { _isRead = value; NotifyPropertyChanged(nameof(IsRead)); }
        }
        private bool _isStarred;
        public bool IsStarred
        {
            get { return _isStarred; }
            set { _isStarred = value; NotifyPropertyChanged(nameof(IsStarred)); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged(nameof(Title)); }
        }

        public string Subtitle
        {
            get
            {
                if (Author != "")
                {
                    return FeedTitle + " - " + Author;
                }
                else
                {
                    return FeedTitle;
                }
            }
        }

        private string _author;
        public string Author
        {
            get
            {
                return _author;
            }
            set
            {
                _author = value;
                NotifyPropertyChanged(nameof(Author));
                NotifyPropertyChanged(nameof(Subtitle));
            }
        }

        public string ContentHeader { get; set; } // Article title, subtitle and date placed above article content
        public string Content { get; set; } // HTML content from the RSS feed
        public string OriginalText { get; set; } // Result of NewsBlur's "Original Text"

        public string FeedContent { get { return Content; } }
        public string TextContent
        {
            get
            {
                return (OriginalText != null) ? OriginalText : "<div>Failed to get the story's original text.</div>";
            }
        }

        // Article content bound to the webview
        private string viewContent;
        public string ViewContent
        {
            get { return viewContent; }
            set { viewContent = value; NotifyPropertyChanged(nameof(ViewContent)); }
        }

        // Article theming

        private Color webViewBackgroundColor;
        public Color WebViewBackgroundColor
        {
            get { return webViewBackgroundColor; }
            set { webViewBackgroundColor = value; NotifyPropertyChanged(nameof(WebViewBackgroundColor)); }
        }

        private Color contentBackgroundColor;
        public Color ContentBackgroundColor
        {
            get { return contentBackgroundColor; }
            set { contentBackgroundColor = value; NotifyPropertyChanged(nameof(ContentBackgroundColor)); }
        }

        // Null color => use normal theme value
        private SolidColorBrush contentForegroundBrush;
        public SolidColorBrush ContentForegroundBrush
        {
            get { return contentForegroundBrush; }
            set { contentForegroundBrush = value; NotifyPropertyChanged(nameof(ContentForegroundBrush)); }
        }

        private int contentTextSize = App.Settings.ReadingTextSize;
        public int ContentTextSize
        {
            get { return contentTextSize; }
            set { contentTextSize = value; NotifyPropertyChanged(nameof(ContentTextSize)); }
        }

        private double contentLineHeight = App.Settings.ReadingLineHeight;
        public double ContentLineHeight
        {
            get { return contentLineHeight; }
            set { contentLineHeight = value; NotifyPropertyChanged(nameof(ContentLineHeight)); }
        }

        private int contentColumnWidth = App.Settings.ReadingColumnWidth;
        public int ContentColumnWidth
        {
            get { return contentColumnWidth; }
            set { contentColumnWidth = value; NotifyPropertyChanged(nameof(ContentColumnWidth)); }
        }

        public long Timestamp { get; set; }
        public string ShortDate { get; set; }
        public string LongDate { get; set; }
        public List<string> Tags { get; set; }
        public int Intelligence { get; set; }
        public bool ShowImagePreview { get { return App.Settings.ShowImagePreviews; } }
        public Uri ImageThumbnail { get; set; }

        #endregion

        #region Feed Info

        public int FeedID { get; set; }

        private string _feedTitle;
        public string FeedTitle
        {
            get
            {
                return _feedTitle;
            }
            set
            {
                _feedTitle = value;
                NotifyPropertyChanged(nameof(FeedTitle));
                NotifyPropertyChanged(nameof(Subtitle));
            }
        }

        public Color FaviconColor { get; set; }
        public Color FaviconFadeColor { get; set; }

        #endregion

        #region Social Info

        public bool IsShared { get; set; }
        public string SharedComments { get; set; }
        public int SourceUserID { get; set; }
        public List<int> FriendUserIDs { get; set; }

        public bool HasShares { get { return ShareCount > 0; } }
        public int ShareCount { get; set; }
        public List<Comment> FriendComments { get; set; }
        public List<Comment> FriendShares { get; set; }
        public List<Comment> PublicComments { get; set; }
        public List<int> PublicShares { get; set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}