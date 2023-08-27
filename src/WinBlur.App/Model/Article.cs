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
            set { _isRead = value; NotifyPropertyChanged("IsRead"); }
        }
        private bool _isStarred;
        public bool IsStarred
        {
            get { return _isStarred; }
            set { _isStarred = value; NotifyPropertyChanged("IsStarred"); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged("Title"); }
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
                NotifyPropertyChanged("Author");
                NotifyPropertyChanged("Subtitle");
            }
        }

        public string ContentHeader { get; set; } // Article title, subtitle and date placed above article content
        public string Content { get; set; } // HTML content from the RSS feed
        public string OriginalText { get; set; } // Result of NewsBlur's "Original Text"

        public string FeedContent { get { return string.Concat(ContentHeader, Content); } }
        public string TextContent
        {
            get
            {
                return (OriginalText != null) ?
                    string.Concat(ContentHeader, OriginalText) :
                    string.Concat(ContentHeader, "<div>Failed to get the story's original text.</div>");
            }
        }

        // Article content bound to the webview
        private string viewContent;
        public string ViewContent
        {
            get { return viewContent; }
            set { viewContent = value; NotifyPropertyChanged("ViewContent"); }
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
                NotifyPropertyChanged("FeedTitle");
                NotifyPropertyChanged("Subtitle");
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