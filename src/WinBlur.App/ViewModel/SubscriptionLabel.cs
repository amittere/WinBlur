using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace WinBlur.App.ViewModel
{
    public enum SubscriptionType
    {
        Global,
        Social,
        Site,
        Saved
    }

    public class SubscriptionLabel : INotifyPropertyChanged
    {
        public int ID { get; set; }

        public Uri PhotoUri { get; set; }
        public string FolderIcon { get; set; }
        public Symbol Glyph { get; set; }
        public IconElement Icon
        {
            get
            {
                if (PhotoUri != null)
                {
                    var image = new BitmapImage { DecodePixelWidth = 100, UriSource = PhotoUri };
                    return new ImageIcon { Source = image };
                }
                else
                {
                    return new SymbolIcon { Symbol = Glyph };
                }
            }
        }

        public SubscriptionType Type { get; set; }

        public int FolderDepth { get; set; }
        public bool IsFolder { get; set; }
        public string ParentFolder { get; set; }

        public SubscriptionLabel ParentLabel { get; set; }
        public ObservableCollection<SubscriptionLabel> Children { get; private set; }
        public ObservableCollection<SubscriptionLabel> FilteredChildren { get; private set; }

        /// <summary>
        /// Collection of Feed IDs that are inside this folder.
        /// Includes all feeds inside it, regardless of folder depth.
        /// </summary>
        public ObservableCollection<int> Subitems { get; set; }

        public bool IsCompressible
        {
            get
            {
                // All folders EXCEPT the "Global Shared Stories" label is compressible
                return IsFolder && Title != "Global Shared Stories";
            }
        }

        private bool _isCompressed;
        public bool IsCompressed
        {
            get
            {
                // Only compressible folders have a valid value
                if (IsCompressible)
                {
                    return _isCompressed;
                }
                return false;
            }
            set
            {
                // Only compressible folders can update the value
                if (IsCompressible && _isCompressed != value)
                {
                    _isCompressed = value;

                    // Save setting so collapsed state persists across launch
                    App.Settings.SetFolderCompress(ToString(), _isCompressed);

                    NotifyPropertyChanged("IsCompressed");
                    NotifyPropertyChanged("ShowCounts");
                }
            }
        }

        public bool IsUnderCompressedFolder
        {
            get
            {
                // Check all parent folders to see if any of them have been compressed
                SubscriptionLabel parent = ParentLabel;
                while (parent != null)
                {
                    if (parent.IsCompressed)
                    {
                        return true;
                    }
                    parent = parent.ParentLabel;
                }
                return false;
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged("Title"); }
        }

        public bool ShowCounts
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.Global:
                        return false;

                    case SubscriptionType.Saved:
                        return true;

                    case SubscriptionType.Social:
                    case SubscriptionType.Site:
                    default:
                        if (IsFolder)
                        {
                            return IsCompressed;
                        }
                        else
                        {
                            return true;
                        }
                }
            }
        }

        public bool HasUnreadStories
        {
            get
            {
                return UnreadCount > 0;
            }
        }

        public int UnreadCount
        {
            get { return PsCount + NtCount; }
        }

        public int PsCount
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Friends.ContainsKey(id) ? App.Client.Friends[id].PsCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Friends.ContainsKey(ID) ? App.Client.Friends[ID].PsCount : 0;
                        }

                    case SubscriptionType.Site:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Feeds.ContainsKey(id) ? App.Client.Feeds[id].PsCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Feeds.ContainsKey(ID) ? App.Client.Feeds[ID].PsCount : 0;
                        }

                    default:
                        // Global Shared Stories has no unread counts
                        // All saved stories only have NtCount
                        return 0;
                }
            }
            set
            {
                // Set the Model's value, but only if it's not a folder
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (!IsFolder && App.Client.Friends.ContainsKey(ID))
                        {
                            App.Client.Friends[ID].PsCount = value;
                            NotifyPropertyChanged("PsCount");
                        }
                        break;

                    case SubscriptionType.Site:
                        if (!IsFolder && App.Client.Feeds.ContainsKey(ID))
                        {
                            App.Client.Feeds[ID].PsCount = value;
                            NotifyPropertyChanged("PsCount");
                        }
                        break;

                    default:
                        // Global Shared Stories and saved stories don't have updates
                        break;
                }
            }
        }

        public int NtCount
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Friends.ContainsKey(id) ? App.Client.Friends[id].NtCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Friends.ContainsKey(ID) ? App.Client.Friends[ID].NtCount : 0;
                        }

                    case SubscriptionType.Site:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Feeds.ContainsKey(id) ? App.Client.Feeds[id].NtCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Feeds.ContainsKey(ID) ? App.Client.Feeds[ID].NtCount : 0;
                        }

                    case SubscriptionType.Saved:
                        if (IsFolder)
                        {
                            return App.Client.SavedStoryCount;
                        }
                        else
                        {
                            return App.Client.SavedStoryTags.ContainsKey(Title) ? App.Client.SavedStoryTags[Title] : 0;
                        }

                    default:
                        // Global Shared Stories has no unread counts
                        return 0;
                }
            }
            set
            {
                // Set the Model's value, but only if it's not a folder
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (!IsFolder && App.Client.Friends.ContainsKey(ID))
                        {
                            App.Client.Friends[ID].NtCount = value;
                            NotifyPropertyChanged("NtCount");
                        }
                        break;

                    case SubscriptionType.Site:
                        if (!IsFolder && App.Client.Feeds.ContainsKey(ID))
                        {
                            App.Client.Feeds[ID].NtCount = value;
                            NotifyPropertyChanged("NtCount");
                        }
                        break;

                    case SubscriptionType.Saved:
                        if (!IsFolder && App.Client.SavedStoryTags.ContainsKey(Title))
                        {
                            App.Client.SavedStoryTags[Title] = value;
                            NotifyPropertyChanged("NtCount");
                        }
                        break;

                    default:
                        // Global Shared Stories don't have updates
                        break;
                }
            }
        }

        public int NgCount
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Friends.ContainsKey(id) ? App.Client.Friends[id].NgCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Friends.ContainsKey(ID) ? App.Client.Friends[ID].NgCount : 0;
                        }

                    case SubscriptionType.Site:
                        if (IsFolder)
                        {
                            return Subitems.Sum(id =>
                            {
                                return App.Client.Feeds.ContainsKey(id) ? App.Client.Feeds[id].NgCount : 0;
                            });
                        }
                        else
                        {
                            return App.Client.Feeds.ContainsKey(ID) ? App.Client.Feeds[ID].NgCount : 0;
                        }

                    default:
                        // Global Shared Stories has no unread counts
                        // All saved stories only have NtCount
                        return 0;
                }
            }
            set
            {
                // Set the Model's value, but only if it's not a folder
                switch (Type)
                {
                    case SubscriptionType.Social:
                        if (!IsFolder && App.Client.Friends.ContainsKey(ID))
                        {
                            App.Client.Friends[ID].NgCount = value;
                            NotifyPropertyChanged("NgCount");
                        }
                        break;

                    case SubscriptionType.Site:
                        if (!IsFolder && App.Client.Feeds.ContainsKey(ID))
                        {
                            App.Client.Feeds[ID].NgCount = value;
                            NotifyPropertyChanged("NgCount");
                        }
                        break;

                    default:
                        // Global Shared Stories and saved stories don't have updates
                        break;
                }
            }
        }

        public SubscriptionLabel()
        {
            Children = new ObservableCollection<SubscriptionLabel>();
            FilteredChildren = new ObservableCollection<SubscriptionLabel>();
            Subitems = new ObservableCollection<int>();
            FolderDepth = 0;
            ParentFolder = null;
            ParentLabel = null;
            IsCompressed = false;
            PhotoUri = null;
        }

        #region Object

        public override string ToString()
        {
            return string.Format("SubscriptionLabel:ID={0};Title={1};Type={2};Parent={3}",
                ID, Title, (int)Type, ParentFolder == null ? "" : ParentFolder);
        }

        public override bool Equals(object obj)
        {
            if (obj is SubscriptionLabel other)
            {
                return (this.ID == other.ID) &&
                       (this.Title == other.Title) &&
                       (this.Type == other.Type) &&
                       (this.ParentFolder == other.ParentFolder);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));

                // Also notify parent label if one is present
                if (ParentLabel != null)
                {
                    ParentLabel.NotifyPropertyChanged(propertyName);
                }
            }
        }

        #endregion
    }
}