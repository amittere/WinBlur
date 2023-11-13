using System;
using System.ComponentModel;
using Windows.UI;

namespace WinBlur.App.Model
{
    public class Feed : INotifyPropertyChanged
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public Uri WebsiteURL { get; set; }
        public Uri FeedURL { get; set; }

        public bool Active { get; set; }
        
        private int _psCount;
        public int PsCount
        {
            get { return _psCount; }
            set { _psCount = value; NotifyPropertyChanged(nameof(PsCount)); }
        }

        private int _ntCount;
        public int NtCount
        {
            get { return _ntCount; }
            set { _ntCount = value; NotifyPropertyChanged(nameof(NtCount)); }
        }

        private int _ngCount;
        public int NgCount
        {
            get { return _ngCount; }
            set { _ngCount = value; NotifyPropertyChanged(nameof(NgCount)); }
        }

        public Uri FaviconURL { get; set; }
        public Color FaviconColor { get; set; }
        public Color FaviconFadeColor { get; set; }
        public Color FaviconBorderColor { get; set; }

        public Feed() { }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SocialFeed : Feed
    {
        public Uri PhotoURL { get; set; }
        public string Username { get; set; }
    }
}
