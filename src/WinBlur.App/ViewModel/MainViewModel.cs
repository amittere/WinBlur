using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.Model;
using WinBlur.Shared;

namespace WinBlur.App.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        public TestModeHelper TestModeHelper { get { return App.TestModeHelper; } }

        public DispatcherTimer SyncUnreadCountTimer { get; private set; }

        public ObservableCollection<SubscriptionLabel> Subscriptions { get; set; }
        public ObservableCollection<SubscriptionLabel> FilteredSubscriptions { get; set; }
        public ObservableCollection<SiteAutoCompleteEntry> SiteAutoCompleteList { get; set; }
        public ObservableCollection<FolderLabel> FolderList { get; set; }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; NotifyPropertyChanged(nameof(Username)); }
        }

        private SubscriptionLabel _selectedSubscription;
        public SubscriptionLabel SelectedSubscription
        {
            get
            {
                return _selectedSubscription;
            }
            set
            {
                _selectedSubscription = value;
                FilterSubscriptions(FeedMode);
                NotifyPropertyChanged(nameof(SelectedSubscription));
            }
        }

        public FeedMode FeedMode
        {
            get { return App.Settings.FeedFilterMode; }
            set
            {
                // Update setting
                if (App.Settings.FeedFilterMode != value)
                {
                    App.Settings.FeedFilterMode = value;
                }
                FilterSubscriptions(value);
                NotifyPropertyChanged(nameof(FeedMode));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; NotifyPropertyChanged(nameof(IsLoading)); }
        }

        // Keep track of whether we are actively filtering subscriptions,
        // so that we don't save the compressed state of folders when they are changed programmatically
        // (e.g. when filtering subscriptions).
        private bool _isFiltering = false;

        // Total Site Unread Count tracking (for app badge)
        private SubscriptionLabel allSitesLabel;

        #endregion Fields

        #region Init

        public MainViewModel()
        {
            Username = null;
            Subscriptions = new ObservableCollection<SubscriptionLabel>();
            FilteredSubscriptions = new ObservableCollection<SubscriptionLabel>();
            SiteAutoCompleteList = new ObservableCollection<SiteAutoCompleteEntry>();
            FolderList = new ObservableCollection<FolderLabel>();
            SelectedSubscription = null;
            IsLoading = false;

            App.Client.UnreadCountsChanged += UnreadCountsChanged;

            SyncUnreadCountTimer = new DispatcherTimer();
            SyncUnreadCountTimer.Tick += SyncUnreadCountTimer_Tick;
            SyncUnreadCountTimer.Interval = new TimeSpan(0, 2, 0); // 2 minutes
        }

        #endregion Init

        #region Subscriptions

        public async Task SyncSubscriptionListAsync()
        {
            IsLoading = true;

            try
            {
                string s = await App.Client.GetSiteList();
                if (App.Client.ParseFeeds(s))
                {
                    ParseFeeds(s);

                    // TODO: if we're syncing with a feed selected, restore that selection
                    await SyncUnreadCountsAsync();

                    // If the user has no feeds (i.e. only Global Shared), display noFeedsText
                    //if (viewModel.FilteredSubscriptions.Count <= 1)
                    //{
                    //    noFeedsText.Visibility = Visibility.Visible;
                    //}
                    //else
                    //{
                    //    noFeedsText.Visibility = Visibility.Collapsed;
                    //}
                }
                else
                {
                    DialogHelper.DisplaySyncError();
                }
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }

            IsLoading = false;
        }

        private void ParseFeeds(string response)
        {
            JObject pResponse = JObject.Parse(response);

            if (pResponse["social_profile"] is JObject profile)
            {
                Username = ParseHelper.ParseValueRef(profile["username"], "");
            }

            Subscriptions.Clear();

            // Add "Global Site Stories" header
            SubscriptionLabel global = new SubscriptionLabel
            {
                Title = "Global Shared Stories",
                IsFolder = true,
                Glyph = Symbol.World,
                FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.Globe),
                Type = SubscriptionType.Global
            };
            Subscriptions.Add(global);

            // Parse friends
            if (pResponse["social_feeds"] is JArray friends && friends.Count > 0)
            {
                SubscriptionLabel allShared = new SubscriptionLabel()
                {
                    Title = "All Shared Stories",
                    IsFolder = true,
                    Glyph = Symbol.People,
                    FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.People),
                    Type = SubscriptionType.Social
                };
                SetCompressedState(allShared);
                Subscriptions.Add(allShared);
                ParseFriends(friends, allShared);
            }

            // Always add the Top Level folder here so that brand new accounts can still add folders
            FolderList.Clear();
            FolderList.Add(new FolderLabel("Top Level", null, 0));

            // Parse subscriptions
            if (pResponse["folders"] is JArray folders && folders.Count > 0)
            {
                SubscriptionLabel allSites = new SubscriptionLabel
                {
                    Title = "All Site Stories",
                    IsFolder = true,
                    Glyph = Symbol.PreviewLink,
                    FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.Sites),
                    Type = SubscriptionType.Site
                };
                Subscriptions.Add(allSites);
                allSitesLabel = allSites;
                ParseFolder(folders, allSites, 0);
            }

            // Parse saved
            int savedCount = ParseHelper.ParseValueStruct(pResponse["starred_count"], 0);
            if (savedCount > 0)
            {
                SubscriptionLabel allSaved = new SubscriptionLabel()
                {
                    Title = "All Saved Stories",
                    IsFolder = true,
                    Glyph = Symbol.OutlineStar,
                    FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.OutlineStar),
                    Type = SubscriptionType.Saved
                };
                SetCompressedState(allSaved);
                Subscriptions.Add(allSaved);

                foreach (var tag in App.Client.SavedStoryTags)
                {
                    allSaved.Children.Add(new SubscriptionLabel()
                    {
                        Title = tag.Key,
                        IsFolder = false,
                        Glyph = Symbol.Tag,
                        FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.Tag),
                        ParentFolder = allSaved.Title,
                        ParentLabel = allSaved,
                        Type = SubscriptionType.Saved
                    });
                }
            }

            FilteredSubscriptions.Clear();
            FilterSubscriptions(App.Settings.FeedFilterMode);
        }

        private void ParseFriends(JArray friends, SubscriptionLabel folder)
        {
            // First pass, build a list of all friends in the folder
            List<SubscriptionLabel> temp = new List<SubscriptionLabel>();
            foreach (JToken t in friends.Children())
            {
                int id = ParseHelper.ParseValueStruct(t["user_id"], -1);
                if (App.Client.Friends.ContainsKey(id))
                {
                    SocialFeed f = App.Client.Friends[id];
                    var label = new SubscriptionLabel()
                    {
                        ID = f.ID,
                        Title = f.Title,
                        IsFolder = false,
                        ParentFolder = folder.Title,
                        ParentLabel = folder,
                        PhotoUri = f.PhotoURL,
                        Glyph = Symbol.Contact,
                        Type = SubscriptionType.Social
                    };
                    SetCompressedState(label);
                    temp.Add(label);
                }
            }

            // Alphabetize the feeds by title, then add them to the actual subscription list
            IEnumerable<SubscriptionLabel> sorted = temp.OrderBy(label => label.Title);
            foreach (SubscriptionLabel l in sorted)
            {
                folder.Children.Add(l);
                folder.Subitems.Add(l.ID);
            }
        }

        private void ParseFolder(JArray array, SubscriptionLabel folder, int depth)
        {
            // First pass, go add all the sites in the folder
            List<SubscriptionLabel> temp = new List<SubscriptionLabel>();
            foreach (JToken t in array.Children())
            {
                if (t.Type == JTokenType.Integer)
                {
                    int idTemp = (int)t;
                    if (App.Client.Feeds.ContainsKey(idTemp))
                    {
                        Feed f = App.Client.Feeds[idTemp];
                        if (f.Active)
                        {
                            temp.Add(new SubscriptionLabel
                            {
                                ID = idTemp,
                                Title = f.Title,
                                FolderDepth = depth,
                                IsFolder = false,
                                ParentFolder = (folder == null) ? "Top Level" : folder.Title,
                                ParentLabel = folder,
                                Glyph = Symbol.Globe,
                                FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.Web),
                                PhotoUri = f.FaviconURL,
                                Type = SubscriptionType.Site
                            });
                        }
                    }
                }
            }

            // Alphabetize the feeds by title, then add them to the actual subscription list
            IEnumerable<SubscriptionLabel> sorted = temp.OrderBy(label => label.Title);
            foreach (SubscriptionLabel l in sorted)
            {
                if (depth == 0)
                {
                    Subscriptions.Add(l);
                }
                else
                {
                    folder.Children.Add(l);
                }
                folder.Subitems.Add(l.ID);
            }

            // Second pass, go through all subfolders, parse them
            List<Tuple<SubscriptionLabel, JArray>> tempFolders = new List<Tuple<SubscriptionLabel, JArray>>();
            foreach (JToken t in array.Children())
            {
                if (t.Type == JTokenType.Object)
                {
                    JProperty innerFolder = (JProperty)t.First;
                    var label = new SubscriptionLabel
                    {
                        Title = innerFolder.Name,
                        FolderDepth = depth,
                        IsFolder = true,
                        ParentFolder = (folder == null) ? "Top Level" : folder.Title,
                        ParentLabel = folder,
                        Glyph = Symbol.Folder,
                        FolderIcon = Converters.IconGlyphToString(Converters.IconGlyph.Folder),
                        Type = SubscriptionType.Site
                    };
                    SetCompressedState(label);
                    tempFolders.Add(new Tuple<SubscriptionLabel, JArray>(label, (JArray)innerFolder.Value));
                }
            }

            // Alphabetize the folders, then add them to the actual subscription list
            IEnumerable<Tuple<SubscriptionLabel, JArray>> sortedFolders = tempFolders.OrderBy(tuple => tuple.Item1.Title);
            foreach (Tuple<SubscriptionLabel, JArray> tuple in sortedFolders)
            {
                SubscriptionLabel label = tuple.Item1;
                JArray folderContents = tuple.Item2;

                if (depth == 0)
                {
                    Subscriptions.Add(label);
                }
                else
                {
                    folder.Children.Add(label);
                }
                FolderList.Add(new FolderLabel(label.Title, label.ParentFolder, label.FolderDepth + 1));

                ParseFolder(folderContents, label, depth + 1);

                foreach (int id in label.Subitems)
                {
                    folder.Subitems.Add(id);
                }
            }
        }

        private void SetCompressedState(SubscriptionLabel label)
        {
            App.Settings.FolderCompressSettings.TryGetValue(label.ToString(), out bool isCompressed);
            label.IsCompressed = isCompressed;
        }

        public void SaveCompressedStateForLabel(SubscriptionLabel label)
        {
            if (!_isFiltering && label.IsCompressible)
            {
                App.Settings.SetFolderCompress(label.ToString(), label.IsCompressed);
            }
        }

        private bool FilterFeed(FeedMode mode, SubscriptionLabel label)
        {
            switch (mode)
            {
                case FeedMode.Unread:
                    if (label == null) return false;
                    switch (label.Type)
                    {
                        case SubscriptionType.Global:
                        case SubscriptionType.Saved:
                            return true;
                        case SubscriptionType.Social:
                            return (label.ParentLabel == null ||
                                    label.ID == App.Client.MyUserID ||
                                    label.Equals(SelectedSubscription) ||
                                    label.HasUnreadStories);
                        case SubscriptionType.Site:
                            return (label.ParentLabel == null ||
                                    label.Equals(SelectedSubscription) ||
                                    label.HasUnreadStories);
                        default:
                            return false;
                    }

                case FeedMode.All:
                default:
                    return true;
            }
        }

        private bool FilterFolder(FeedMode mode, SubscriptionLabel label)
        {
            switch (mode)
            {
                case FeedMode.Unread:
                    if (label == null) return false;
                    switch (label.Type)
                    {
                        case SubscriptionType.Global:
                        case SubscriptionType.Saved:
                        case SubscriptionType.Social:
                            return true;
                        case SubscriptionType.Site:
                            return (label.FilteredChildren.Count > 0 ||
                                    label.Equals(SelectedSubscription));
                        default:
                            return false;
                    }

                case FeedMode.All:
                default:
                    return true;
            }
        }

        private void FilterSubscriptions(FeedMode mode, SubscriptionLabel label = null)
        {
            _isFiltering = true;
            FilterSubscriptionsHelper(mode, label);

            // Work around a platform issue where sometimes the TreeView loses its selection after filtering.
            NotifyPropertyChanged(nameof(SelectedSubscription));
            _isFiltering = false;
        }

        public bool FilterSubscriptionsHelper(FeedMode mode, SubscriptionLabel label)
        {
            IEnumerable<SubscriptionLabel> children;
            ObservableCollection<SubscriptionLabel> filteredChildren;
            if (label == null)
            {
                children = Subscriptions;
                filteredChildren = FilteredSubscriptions;
            }
            else
            {
                children = label.Children;
                filteredChildren = label.FilteredChildren;
            }

            // Base case
            if (children.Count() == 0)
            {
                return FilterFeed(mode, label);
            }
            else
            {
                // Go through both the full subscription list and the current filter.
                // Remove entries that shouldn't be there, and add ones that should if they are not present.
                int idx1 = 0;
                int idx2 = 0;
                while (idx1 < children.Count() || idx2 < filteredChildren.Count())
                {
                    SubscriptionLabel label1, label2;
                    if (idx1 == children.Count())
                    {
                        // We got to the end of Subscriptions first. This shouldn't happen,
                        // but just in case, check the remaining items for removal
                        label2 = filteredChildren.ElementAt(idx2);
                        if (!FilterSubscriptionsHelper(mode, label2))
                        {
                            filteredChildren.RemoveAt(idx2);
                        }
                        continue; // Continue here so we don't increment idx1 past Count
                    }
                    else if (idx2 == filteredChildren.Count())
                    {
                        // We got to the end of FilteredSubscriptions.
                        // All remaining items in Subscriptions are guaranteed
                        // to not be present in FilteredSubscriptions.
                        label1 = children.ElementAt(idx1);
                        if (FilterSubscriptionsHelper(mode, label1))
                        {
                            // Current item should be added
                            filteredChildren.Insert(idx2, label1);
                            idx2++;
                        }
                    }
                    else
                    {
                        label1 = children.ElementAt(idx1);
                        label2 = filteredChildren.ElementAt(idx2);

                        if (label1.Equals(label2))
                        {
                            if (FilterSubscriptionsHelper(mode, label1))
                            {
                                // Current item should stay.
                                idx2++;
                            }
                            else
                            {
                                // Current item should be removed.
                                filteredChildren.RemoveAt(idx2);
                            }
                        }
                        else
                        {
                            if (FilterSubscriptionsHelper(mode, label1))
                            {
                                // Current item should be added.
                                filteredChildren.Insert(idx2, label1);
                                idx2++;
                            }
                            // else: Current item should be skipped.
                        }
                    }
                    idx1++;
                }

                return FilterFolder(mode, label);
            }
        }

        private async void SyncUnreadCountTimer_Tick(object sender, object e)
        {
            try
            {
                string s = await App.Client.GetUnreadCounts();
                if (App.Client.ParseUnreadCounts(s))
                {
                    UpdateUnreadCounts();
                }
            }
            catch (Exception)
            {
                // Don't throw any error here since this isn't a user-initiated action
            }
        }

        public async Task SyncUnreadCountsAsync()
        {
            try
            {
                string s = await App.Client.GetUnreadCounts();
                if (App.Client.ParseUnreadCounts(s))
                {
                    UpdateUnreadCounts();
                }
                else
                {
                    DialogHelper.DisplaySyncError();
                }
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }
        }

        private void UnreadCountsChanged(object sender, EventArgs e)
        {
            UpdateUnreadCounts();
        }

        public void UpdateUnreadCounts()
        {
            foreach (SubscriptionLabel label in Subscriptions)
            {
                UpdateUnreadCounts(label);
            }

            // Re-filter subscriptions
            FilterSubscriptions(App.Settings.FeedFilterMode);

            UpdateUnreadCountBadge();
        }

        private void UpdateUnreadCounts(SubscriptionLabel label)
        {
            foreach (SubscriptionLabel child in label.Children)
            {
                UpdateUnreadCounts(child);
            }

            label.NotifyPropertyChanged("PsCount");
            label.NotifyPropertyChanged("NtCount");
            label.NotifyPropertyChanged("NgCount");
            label.NotifyPropertyChanged("UnreadCount");
            label.NotifyPropertyChanged("HasUnreadStories");
        }

        private void UpdateUnreadCountBadge()
        {
            // Update app badge based on "All Site Stories" unread count
            if (allSitesLabel != null)
            {
                BadgeHelper.UpdateNumericBadge((uint)allSitesLabel.UnreadCount);
            }
        }

        public async Task MarkAllAsReadAsync(int numDaysBack)
        {
            await App.Client.MarkAllAsRead(numDaysBack);
            await SyncUnreadCountsAsync();
        }

        public void ParseSiteAutoComplete(string response)
        {
            SiteAutoCompleteList.Clear();
            JArray pResponse = JArray.Parse(response);
            foreach (JToken t in pResponse.Children())
            {
                SiteAutoCompleteEntry entry = new SiteAutoCompleteEntry
                {
                    ID = ParseHelper.ParseValueStruct<int>(t["id"], -1),
                    Title = ParseHelper.ParseValueRef<string>(t["label"], null),
                    FeedURL = ParseHelper.ParseValueRef<string>(t["value"], null),
                    NumSubscribers = ParseHelper.ParseValueStruct<int>(t["num_subscribers"], 0),
                };

                // Add to autocomplete list
                SiteAutoCompleteList.Add(entry);
            }
        }

        public async Task AddSiteAsync(string url, string folder)
        {
            string response = await App.Client.AddSite(url, folder);
            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Please check your URL and try again.");
            }
            else
            {
                await SyncSubscriptionListAsync();
            }
        }

        public async Task AddFolderAsync(string folder, string parentFolder)
        {
            string response = await App.Client.AddFolder(folder, parentFolder);
            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
            else
            {
                await SyncSubscriptionListAsync();
            }
        }

        public async Task DeleteItemAsync(SubscriptionLabel label)
        {
            string parentFolder = label.ParentFolder;
            if (parentFolder == "Top Level")
            {
                parentFolder = "";
            }

            string response;
            if (label.IsFolder)
            {
                response = await App.Client.RemoveFolder(label.Title, parentFolder, label.Subitems);
            }
            else
            {
                response = await App.Client.RemoveSite(label.ID, parentFolder);
            }

            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
            else
            {
                await SyncSubscriptionListAsync();
            }
        }

        public async Task RenameItemAsync(SubscriptionLabel label, string newName)
        {
            string response;
            if (label.IsFolder)
            {
                response = await App.Client.RenameFolder(label.Title, newName, label.ParentFolder);
            }
            else
            {
                response = await App.Client.RenameSite(label.ID, newName);
            }

            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
            else
            {
                // Just rename the site locally
                label.Title = newName;
            }
        }

        public async Task MoveItemAsync(SubscriptionLabel label, string fromFolder, string toFolder)
        {
            // Fixup top level case
            if (fromFolder == "Top Level")
            {
                fromFolder = "";
            }
            if (toFolder == "Top Level")
            {
                toFolder = "";
            }

            string response;
            if (label.IsFolder)
            {
                response = await App.Client.MoveFolder(label.Title, fromFolder, toFolder);
            }
            else
            {
                response = await App.Client.MoveSite(label.ID, fromFolder, toFolder);
            }

            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
            else
            {
                await SyncSubscriptionListAsync();
            }
        }

        #endregion Subscriptions

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
