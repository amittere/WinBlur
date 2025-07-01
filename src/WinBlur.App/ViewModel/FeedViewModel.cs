using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.Model;

namespace WinBlur.App.ViewModel
{
    internal class FeedViewModel : INotifyPropertyChanged
    {
        private readonly ReadArticleQueue readArticleQueue = new ReadArticleQueue();
        private readonly DispatcherTimer markAsReadTimer = new DispatcherTimer();
        private readonly List<int> feeds = new List<int>();

        private SubscriptionLabel subscription;
        public SubscriptionLabel Subscription
        {
            get
            {
                return subscription;
            }
            set
            {
                subscription = value;
                feeds.Clear();
                if (Subscription.IsFolder)
                {
                    feeds.AddRange(subscription.Subitems);
                }
                else
                {
                    feeds.Add(subscription.ID);
                }
            }
        }

        public SortMode SortMode
        {
            get
            {
                if (Subscription.IsFolder)
                {
                    return App.Settings.SortModeSettings.GetValueOrDefault(Subscription.Title, App.Settings.FolderSortMode);
                }
                else
                {
                    return App.Settings.SortModeSettings.GetValueOrDefault(Subscription.ID.ToString(), App.Settings.FeedSortMode);
                }
            }
            set
            {
                if (Subscription.IsFolder)
                {
                    if (App.Settings.SortModeSettings.ContainsKey(Subscription.Title) ||
                        value != App.Settings.FolderSortMode)
                    {
                        // Only save a new setting if it is different from the default.
                        // NOTE: there is a bug here where all folders with the same title
                        // will have the same setting. Not sure how to work around this.
                        App.Settings.SetSortMode(Subscription.Title, value);
                    }
                }
                else
                {
                    string idStr = Subscription.ID.ToString();
                    if (App.Settings.SortModeSettings.ContainsKey(idStr) ||
                        value != App.Settings.FeedSortMode)
                    {
                        // Only save a new setting if it is different from the default.
                        App.Settings.SetSortMode(idStr, value);
                    }
                }
                NotifyPropertyChanged(nameof(SortMode));
            }
        }

        public bool SortModeIsNewestFirst { get { return SortModeHelpers.IsNewestFirst(SortMode); } }

        public ReadingMode ReadingMode
        {
            get
            {
                if (Subscription.IsFolder)
                {
                    return App.Settings.ReadingModeSettings.GetValueOrDefault(Subscription.Title, App.Settings.DefaultReadingMode);
                }
                else
                {
                    return App.Settings.ReadingModeSettings.GetValueOrDefault(Subscription.ID.ToString(), App.Settings.DefaultReadingMode);
                }
            }
            set
            {
                // Save the change into the site settings
                if (Subscription.IsFolder)
                {
                    if (App.Settings.ReadingModeSettings.ContainsKey(Subscription.Title) ||
                        value != App.Settings.DefaultReadingMode)
                    {
                        // Only save a new setting if it is different from the default.
                        // NOTE: there is a bug here where all folders with the same title
                        // will have the same setting. Not sure how to work around this.
                        App.Settings.SetReadingMode(Subscription.Title, value);
                    }
                }
                else
                {
                    string idStr = Subscription.ID.ToString();
                    if (App.Settings.ReadingModeSettings.ContainsKey(idStr) ||
                        value != App.Settings.DefaultReadingMode)
                    {
                        // Only save a new setting if it is different from the default.
                        App.Settings.SetReadingMode(idStr, value);
                    }
                }
                NotifyPropertyChanged(nameof(ReadingMode));
            }
        }

        public int ReadingModeIndex
        {
            get { return (int)ReadingMode; }
            set { ReadingMode = (ReadingMode)value; NotifyPropertyChanged(nameof(ReadingModeIndex)); }
        }

        public int ReadingTextSize
        {
            get => App.Settings.ReadingTextSize;
            set
            {
                App.Settings.ReadingTextSize = value;
                NotifyPropertyChanged(nameof(ReadingTextSize));
                RefreshArticleContent();
            }
        }

        private Article _selectedArticle;
        public Article SelectedArticle
        {
            get { return _selectedArticle; }
            set
            {
                if (App.Settings.MarkAsRead == MarkAsReadMode.WhenSelectionChanges &&
                    _selectedArticle != null)
                {
                    // Mark previously selected article as read before updating
                    ReadArticle(_selectedArticle);
                }

                _selectedArticle = value;
                NotifyPropertyChanged(nameof(SelectedArticle));

                if (App.Settings.MarkAsRead == MarkAsReadMode.AfterDelay)
                {
                    // Stop timer if it is currently running.
                    if (markAsReadTimer.IsEnabled)
                    {
                        markAsReadTimer.Stop();
                    }

                    // Update timer's interval if the setting has changed
                    if (markAsReadTimer.Interval.TotalSeconds != App.Settings.MarkAsReadDelay)
                    {
                        markAsReadTimer.Interval = new TimeSpan(0, 0, App.Settings.MarkAsReadDelay);
                    }

                    // Read article
                    if (_selectedArticle != null)
                    {
                        if (App.Settings.MarkAsReadDelay == 0)
                        {
                            ReadArticle(_selectedArticle);
                        }
                        else
                        {
                            markAsReadTimer.Start();
                        }
                    }
                }
            }
        }

        private ArticleSource articleSource;
        public IncrementalLoadingCollection<ArticleSource, Article> ArticleList { get; set; }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; NotifyPropertyChanged(nameof(IsLoading)); }
        }

        public bool CanMarkAsRead
        {
            get
            {
                return (Subscription.Type == SubscriptionType.Site) ||
                       (Subscription.Type == SubscriptionType.Social && !Subscription.IsFolder);
            }
        }

        public bool NoArticles
        {
            get
            {
                return ArticleList != null && !ArticleList.HasMoreItems && ArticleList.Count == 0;
            }
        }

        private bool commentsVisible = false;
        public bool CommentsVisible
        {
            get { return commentsVisible; }
            set { commentsVisible = value; NotifyPropertyChanged(nameof(CommentsVisible)); }
        }

        private bool isLoadingOriginalText = false;
        public bool IsLoadingOriginalText
        {
            get { return isLoadingOriginalText; }
            set { isLoadingOriginalText = value; NotifyPropertyChanged(nameof(IsLoadingOriginalText)); }
        }

        public FeedViewModel()
        {
            markAsReadTimer.Interval = new TimeSpan(0, 0, App.Settings.MarkAsReadDelay);
            markAsReadTimer.Tick += MarkAsReadTimer_Tick;
        }

        private void MarkAsReadTimer_Tick(object sender, object e)
        {
            markAsReadTimer.Stop();
            ReadArticle(SelectedArticle);
        }

        #region Article Lists

        public void UpdateArticleList(SortMode mode)
        {
            SortMode = mode;

            articleSource = new ArticleSource
            {
                Feeds = feeds,
                Type = Subscription.Type,
                IsFolder = Subscription.IsFolder,
                Tag = (Subscription.Type == SubscriptionType.Saved) ? Subscription.Title : "",
                NewestFirst = SortModeHelpers.IsNewestFirst(mode),
                UnreadOnly = SortModeHelpers.IsUnreadOnly(mode),
            };

            ArticleList = new IncrementalLoadingCollection<ArticleSource, Article>(
                articleSource, 6, OnStartLoading, OnEndLoading, OnErrorLoading);

            NotifyPropertyChanged(nameof(NoArticles));
        }

        private void OnStartLoading()
        {
            IsLoading = true;
        }

        private void OnEndLoading()
        {
            IsLoading = false;
            NotifyPropertyChanged(nameof(NoArticles));
        }

        private void OnErrorLoading(Exception e)
        {
            NotifyPropertyChanged(nameof(NoArticles));
        }

        public async Task MarkArticlesAsReadAsync(long timestamp, bool directionOlder)
        {
            if (ArticleList == null) return;

            // Update UI
            if (directionOlder)
            {
                foreach (Article a in ArticleList)
                {
                    if (a.Timestamp <= timestamp)
                    {
                        App.Client.UpdateReadState(a);
                    }
                }
            }
            else
            {
                foreach (Article a in ArticleList)
                {
                    if (a.Timestamp >= timestamp)
                    {
                        App.Client.UpdateReadState(a);
                    }
                }
            }

            string response = await App.Client.MarkFeedsAsRead(articleSource.Feeds, timestamp, directionOlder);
            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
            else
            {
                // Update unread counts
                response = await App.Client.GetUnreadCounts();
                if (App.Client.ParseUnreadCounts(response))
                {
                    App.Client.NotifyUnreadCountsChanged();
                }
            }
        }

        public async Task MarkAllArticlesAsReadAsync()
        {
            // Update UI now, so that we avoid the bug where if you switch subscriptions fast enough,
            // The new subscription will have its UI updated, when it should be the previous one.
            // This also means that we don't need to re-sync unread counts
            foreach (Article a in ArticleList)
            {
                App.Client.UpdateReadState(a);
            }

            // Save the list of feeds we're marking as read, so if we change the article list underneath
            // we don't mark the wrong feeds as read!
            IEnumerable<int> feedsToMark = articleSource.Feeds;

            // Because updating the read state for each article in the ArticleList may not cover all
            // the articles in the feed, zero out the unread counts for the feeds here.
            foreach (int feedID in feedsToMark)
            {
                if (App.Client.Feeds.ContainsKey(feedID))
                {
                    App.Client.Feeds[feedID].PsCount = 0;
                    App.Client.Feeds[feedID].NtCount = 0;
                    App.Client.Feeds[feedID].NgCount = 0;
                }
                else if (App.Client.Friends.ContainsKey(feedID))
                {
                    App.Client.Friends[feedID].PsCount = 0;
                    App.Client.Friends[feedID].NtCount = 0;
                    App.Client.Friends[feedID].NgCount = 0;
                }
            }
            App.Client.NotifyUnreadCountsChanged();

            // Now mark the original feeds as read
            string response = await App.Client.MarkFeedsAsRead(feedsToMark);
            JObject json = JObject.Parse(response);
            if ((string)json["result"] != "ok")
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }

            // If the setting is set, go to the next site/folder.
            // NOTE: this will cause the parent MainPage to navigate to a new feed.
            if (App.Settings.GoToNextSubscriptionAfterMarkingAsRead)
            {
                App.Client.NotifyFeedMarkedAsRead();
            }
        }

        public void RefreshArticleContent()
        {
            var viewBackgroundColor = ReadingThemeViewModel.GetWebViewBackgroundColorForReadingTheme(App.Settings.ReadingTheme);
            var contentBackgroundColor = ReadingThemeViewModel.GetContentBackgroundColorForReadingTheme(App.Settings.ReadingTheme);
            var contentForegroundColor = ReadingThemeViewModel.GetContentForegroundBrushForReadingTheme(App.Settings.ReadingTheme);
            foreach (Article a in ArticleList)
            {
                a.WebViewBackgroundColor = viewBackgroundColor;
                a.ContentBackgroundColor = contentBackgroundColor;
                a.ContentForegroundBrush = contentForegroundColor;
                a.ContentTextSize = ReadingTextSize;
                a.ViewContent = "";
                if (SelectedArticle == a)
                {
                    a.ViewContent = ReadingMode == ReadingMode.Text ? a.TextContent : a.FeedContent;
                }
            }
        }

        #endregion

        #region Article Detail

        public void ReadArticle(Article a)
        {
            if (!a.IsRead)
            {
                App.Client.UpdateReadState(a);
                App.Client.NotifyUnreadCountsChanged();
                readArticleQueue.AddArticleToQueue(a);
            }
        }

        public async void UnreadArticle(Article a)
        {
            try
            {
                if (a.IsRead)
                {
                    App.Client.UpdateUnreadState(a);
                    App.Client.NotifyUnreadCountsChanged();
                    readArticleQueue.RemoveArticleFromQueue(a);
                    await App.Client.MarkStoryAsUnread(a.Hash);
                }
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }
        }

        public async void StarArticle(Article a)
        {
            try
            {
                a.IsStarred = true;
                await App.Client.MarkStoryAsStarred(a.Hash);
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }
        }

        public async void UnstarArticle(Article a)
        {
            try
            {
                a.IsStarred = false;
                await App.Client.MarkStoryAsUnstarred(a.Hash);
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }
        }

        public async Task<bool> GetOriginalText(Article a)
        {
            if (a.OriginalText == null)
            {
                try
                {
                    IsLoadingOriginalText = true;

                    string originalText = await App.Client.GetOriginalStoryText(a.ID, a.FeedID);

                    IsLoadingOriginalText = false;

                    if (string.IsNullOrWhiteSpace(originalText))
                    {
                        // Something happened, let the caller know
                        return false;
                    }

                    a.OriginalText = originalText;
                }
                catch (Exception)
                {
                    DialogHelper.DisplayNetworkError();
                    return false;
                }
            }

            // Original text has already been populated.
            return true;
        }

        public async Task<Article> ShareArticleAsync(string comment)
        {
            string response = await App.Client.ShareStory(
                SelectedArticle.FeedID, SelectedArticle.ID, comment, SelectedArticle.SourceUserID);

            JObject json = JObject.Parse(response);
            if ((string)json["result"] == "ok")
            {
                // Parse the article out
                return App.Client.ParseArticle(json["story"]);
            }
            else
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
        }

        public async Task<Article> UnshareArticleAsync()
        {
            string response = await App.Client.UnshareStory(SelectedArticle.FeedID, SelectedArticle.ID);
            JObject json = JObject.Parse(response);
            if ((string)json["result"] == "ok")
            {
                // Parse the article out
                return App.Client.ParseArticle(json["story"]);
            }
            else
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
        }

        public async Task ToggleLikeAsync(Comment comment)
        {
            if (comment.IsLiked)
            {
                // Unlike comment
                string response = await App.Client.RemoveLikeComment(
                    SelectedArticle.FeedID, SelectedArticle.ID, comment.User.ID);
                JObject json = JObject.Parse(response);
                if ((string)json["result"] == "ok")
                {
                    // Update UI
                    comment.IsLiked = false;
                }
                else
                {
                    throw new Exception("Something went wrong on NewsBlur's end.");
                }
            }
            else
            {
                // Like comment
                string response = await App.Client.LikeComment(
                    SelectedArticle.FeedID, SelectedArticle.ID, comment.User.ID);
                JObject json = JObject.Parse(response);
                if ((string)json["result"] == "ok")
                {
                    // Update UI
                    comment.IsLiked = true;
                }
                else
                {
                    throw new Exception("Something went wrong on NewsBlur's end.");
                }
            }
        }

        public async Task<Comment> ReplyToCommentAsync(string reply, Comment sourceComment, Comment originalReply)
        {
            string response = await App.Client.SaveCommentReply(
                SelectedArticle.FeedID, SelectedArticle.ID, sourceComment.User.ID, reply, originalReply?.ID);

            JObject json = JObject.Parse(response);
            if ((string)json["result"] == "ok")
            {
                // Parse the new comment
                return App.Client.ParseComment(json["comment"]);
            }
            else
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
        }

        public async Task<Comment> DeleteReplyAsync(Comment sourceComment, Comment originalReply)
        {
            string response = await App.Client.RemoveCommentReply(
                SelectedArticle.FeedID, SelectedArticle.ID, sourceComment.User.ID, originalReply?.ID);

            JObject json = JObject.Parse(response);
            if ((string)json["result"] == "ok")
            {
                // Parse the new comment
                return App.Client.ParseComment(json["comment"]);
            }
            else
            {
                throw new Exception("Something went wrong on NewsBlur's end.");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
