using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.Model;
using WinBlur.App.View;
using WinBlur.App.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinBlur.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedPage : Page
    {
        private FeedViewModel viewModel;

        private List<ReadingThemeViewModel> readingThemes;
        private ReadingThemeViewModel selectedReadingTheme;

        private Article _shareArticle;

        private CommentTextBox activeCommentTextBox;
        private ReplyTextBox activeReplyTextBox;

        private DispatcherTimer loadArticleTimer;

        private IDataTransferManagerInterop dataTransferManagerInterop;
        private DataTransferManager dataTransferManager;
        static readonly Guid s_dataTransferManagerIid = new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        public FeedPage()
        {
            this.InitializeComponent();

            loadArticleTimer = new DispatcherTimer();
            loadArticleTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            loadArticleTimer.Tick += LoadArticleTimer_Tick;

            dataTransferManagerInterop = DataTransferManager.As<IDataTransferManagerInterop>();

            IntPtr result = dataTransferManagerInterop.GetForWindow(App.WindowHandle, s_dataTransferManagerIid);
            dataTransferManager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);

            readingThemes = new List<ReadingThemeViewModel>
            {
                new ReadingThemeViewModel { Label = "System", ThemeMode = ReadingThemeMode.UseWindowsTheme },
                new ReadingThemeViewModel { Label = "Sepia", ThemeMode = ReadingThemeMode.Sepia },
                new ReadingThemeViewModel { Label = "Light", ThemeMode = ReadingThemeMode.Light },
                new ReadingThemeViewModel { Label = "Dark", ThemeMode = ReadingThemeMode.Dark },
                new ReadingThemeViewModel { Label = "Black", ThemeMode = ReadingThemeMode.Black }
            };
            selectedReadingTheme = readingThemes.Find(r => r.ThemeMode == App.Settings.ReadingTheme);
            UpdateReadingTheme();
        }

        /// <summary>
        /// Event handler for the OnNavigatedTo method.
        /// This event occurs every time the page is navigated to.
        /// </summary>
        /// <param name="e">Event parameters</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            viewModel = (FeedViewModel)Resources["viewModel"];
            viewModel.Subscription = (SubscriptionLabel)e.Parameter;
            DataContext = viewModel;

            dataTransferManager.DataRequested += FeedPage_DataRequested;
            App.Settings.ThemeChanged += Settings_ThemeChanged;

            await UpdateArticleListAsync(viewModel.SortMode);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            dataTransferManager.DataRequested -= FeedPage_DataRequested;
            App.Settings.ThemeChanged -= Settings_ThemeChanged;
            base.OnNavigatedFrom(e);
        }

        #region Article List

        private async void ArticleListRefresh_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            await UpdateArticleListAsync(viewModel.SortMode);

            deferral.Complete();
        }

        private async Task UpdateArticleListAsync(SortMode mode)
        {
            UpdateSortModeFlyout(mode);

            // Update the view model's selected article list
            viewModel.UpdateArticleList(mode);

            // Load the first item here before updating the CollectionViewSource.
            // This is a workaround for a bug in the FlipView.
            await viewModel.ArticleList.LoadMoreItemsAsync(6);

            // Now update the CollectionViewSource.
            articleListViewSource.Source = viewModel.ArticleList;
        }

        private void sortModeSplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            sender.Flyout.ShowAt(sender);
        }

        private void UpdateSortModeFlyout(SortMode mode)
        {
            foreach (var item in sortModeMenuFlyout.Items)
            {
                var radioFlyoutItem = (RadioMenuFlyoutItem)item;
                radioFlyoutItem.IsChecked = (SortMode)item.DataContext == mode;
                if (radioFlyoutItem.IsChecked)
                {
                    sortModeSplitButton.Content = radioFlyoutItem.Text;
                }
            }
        }

        private void articleListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (sender is ListView view)
            {
                if (e.ClickedItem is Article a)
                {
                    articleDetailView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding
                    {
                        Source = Resources["articleListViewSource"]
                    });

                    articleDetailView.SelectedItem = a;
                    ViewArticle(a);

                    view.SelectionMode = ListViewSelectionMode.Single;
                    view.IsItemClickEnabled = false;

                    // Add SelectionChanged handler after handling initial click to avoid double view
                    articleDetailView.SelectionChanged += articleDetailView_SelectionChanged;
                }
            }
        }

        private void UnreadFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                viewModel.UnreadArticle(article);
            }
        }

        private void ReadFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                viewModel.ReadArticle(article);
            }
        }

        private void UnsaveFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                viewModel.UnstarArticle(article);
            }
        }

        private void SaveFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                viewModel.StarArticle(article);
            }
        }

        private void ShareFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                _shareArticle = article;
                dataTransferManagerInterop.ShowShareUIForWindow(App.WindowHandle);
            }
        }

        private async void ReadNewerFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                try
                {
                    await viewModel.MarkArticlesAsReadAsync(article.Timestamp, false);
                }
                catch (Exception e)
                {
                    DialogHelper.DisplayErrorMessage("Failed to mark feeds as read", e.Message);
                }
            }
        }

        private async void ReadOlderFlyoutItem_Click(object sender, RoutedEventArgs _)
        {
            if (sender is FrameworkElement element && element.DataContext is Article article)
            {
                try
                {
                    await viewModel.MarkArticlesAsReadAsync(article.Timestamp, true);
                }
                catch (Exception e)
                {
                    DialogHelper.DisplayErrorMessage("Failed to mark feeds as read", e.Message);
                }
            }
        }

        private void ViewArticle(Article a)
        {
            // Note that updating SelectedArticle also marks read state
            viewModel.SelectedArticle = a;
            articleDetailView.SelectedItem = a;

            LoadComments(a);
            LoadArticleContent();
        }

        private void markFeedAsReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.ReadAllPromptEnabled)
            {
                // Show the attached flyout
                if (sender is FrameworkElement element)
                {
                    FlyoutBase.ShowAttachedFlyout(element);
                }
            }
            else
            {
                // Submit
                markFeedAsReadSubmitButton_Click(sender, e);
            }
        }

        private async void markFeedAsReadSubmitButton_Click(object sender, RoutedEventArgs args)
        {
            if (viewModel.ArticleList != null)
            {
                // Hide the attached flyout if present
                FlyoutBase.GetAttachedFlyout(markFeedAsReadButton).Hide();

                try
                {
                    await viewModel.MarkAllArticlesAsReadAsync();
                }
                catch (Exception e)
                {
                    DialogHelper.DisplayErrorMessage("Failed to mark feeds as read", e.Message);
                }
            }
        }

        private async void SortModeFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var item = (RadioMenuFlyoutItem)sender;
            if (item.IsChecked)
            {
                await UpdateArticleListAsync((SortMode)item.DataContext);
            }
        }

        private async void syncFeedButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.ArticleList != null)
            {
                await viewModel.ArticleList.RefreshAsync();
            }
        }

        #endregion

        #region Article Detail

        private void articleDetailView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is FlipView view)
            {
                if (view.SelectedItem is Article a)
                {
                    ViewArticle(a);
                }
            }
        }

        private void starButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a && !a.IsStarred)
            {
                viewModel.StarArticle(a);
            }
        }

        private void unstarButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a && a.IsStarred)
            {
                viewModel.UnstarArticle(a);
            }
        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a && !a.IsRead)
            {
                viewModel.ReadArticle(a);
            }
        }

        private void unreadButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a && a.IsRead)
            {
                viewModel.UnreadArticle(a);
            }
        }

        private async void openInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a && a.ArticleLink != null)
            {
                await Launcher.LaunchUriAsync(a.ArticleLink);
            }
        }

        private void FeedPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (_shareArticle != null)
            {
                DataPackage requestData = args.Request.Data;
                requestData.Properties.Title = _shareArticle.Title;
                requestData.Properties.Description = _shareArticle.FeedTitle;
                requestData.SetWebLink(_shareArticle.ArticleLink);
            }
            else
            {
                args.Request.FailWithDisplayText("Please select an article to share.");
            }
        }

        private void shareButton_Click(object sender, RoutedEventArgs e)
        {
            if (articleDetailView.SelectedItem is Article a)
            {
                _shareArticle = a;
                dataTransferManagerInterop.ShowShareUIForWindow(App.WindowHandle);
            }
        }

        private async void articleTextView_Loaded(object sender, RoutedEventArgs e)
        {
            var webView = (WebView2)sender;
            await webView.EnsureCoreWebView2Async();

            var settings = webView.CoreWebView2.Settings;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.AreDefaultScriptDialogsEnabled = false;
            settings.AreDevToolsEnabled = App.TestModeHelper.TestMode;
            settings.AreHostObjectsAllowed = false;
            settings.IsBuiltInErrorPageEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            settings.IsWebMessageEnabled = false;
        }

        private async void articleTextView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (articleDetailView.SelectedItem is Article && args.IsUserInitiated && args.Uri != null)
            {
                args.Cancel = true;

                // Launch the URL in web browser
                await Launcher.LaunchUriAsync(new Uri(args.Uri));
            }
        }

        #endregion

        #region Reading Mode

        private void LoadArticleTimer_Tick(object sender, object e)
        {
            LoadArticleContent();
        }

        private async void LoadArticleContent()
        {
            bool success = false;

            if (loadArticleTimer.IsEnabled)
            {
                loadArticleTimer.Stop();
            }

            viewModel.IsLoadingOriginalText = false;

            UpdateReadingModeFlyout(viewModel.ReadingMode);

            switch (viewModel.ReadingMode)
            {
                case ReadingMode.Feed:
                    success = LoadFeedView();
                    break;

                case ReadingMode.Text:
                    success = await LoadTextView();
                    break;
            }

            if (!success)
            {
                // Loading the content failed. Try again after a delay...
                loadArticleTimer.Start();
            }
        }

        private async Task<bool> LoadTextView()
        {
            if (GetActiveWebView() != null &&
                articleDetailView?.SelectedItem is Article article)
            {
                // switch to text view, grabbing it from the web if necessary
                await viewModel.GetOriginalText(article);

                article.ViewContent = article.TextContent;
                return true;
            }
            return false;
        }

        private bool LoadFeedView()
        {
            if (GetActiveWebView() != null &&
                articleDetailView?.SelectedItem is Article article)
            {
                article.ViewContent = article.FeedContent;
                return true;
            }
            return false;
        }

        private WebView2 GetActiveWebView()
        {
            if (articleDetailView?.ContainerFromIndex(articleDetailView.SelectedIndex) is FrameworkElement container &&
                container.FindDescendant("articleTextView") is WebView2 view &&
                view.IsLoaded &&
                view.CoreWebView2 != null)
            {
                return view;
            }
            return null;
        }

        private void readingModeSplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            sender.Flyout.ShowAt(sender);
        }

        private void ReadingModeFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var item = (RadioMenuFlyoutItem)sender;
            if (item.IsChecked)
            {
                viewModel.ReadingMode = (ReadingMode)item.DataContext;
                LoadArticleContent();
            }
        }

        private void UpdateReadingModeFlyout(ReadingMode mode)
        {
            foreach (var item in readingModeMenuFlyout.Items)
            {
                var radioFlyoutItem = (RadioMenuFlyoutItem)item;
                radioFlyoutItem.IsChecked = (ReadingMode)item.DataContext == mode;
                if (radioFlyoutItem.IsChecked)
                {
                    readingModeSplitButton.Content = radioFlyoutItem.Text;
                }
            }
        }

        #endregion

        #region Social

        private void commentsButton_Click(object sender, RoutedEventArgs e)
        {
            // Can't change row definitions in XAML Visual States :(
            viewModel.CommentsVisible = !viewModel.CommentsVisible;
            if (viewModel.CommentsVisible)
            {
                CommentsRow.Height = new GridLength(300);
                CommentsRow.MinHeight = 150;
            }
            else
            {
                CommentsRow.Height = new GridLength(0);
                CommentsRow.MinHeight = 0;
            }
        }

        private void LoadComments(Article a)
        {
            if (a == null) return;

            CommentsListView.Items.Clear();

            // Total share count
            TextBlock shareCountText = new TextBlock
            {
                Text = a.ShareCount == 1 ?
                    string.Format("{0} total share", a.ShareCount) :
                    string.Format("{0} total shares", a.ShareCount),
                Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style
            };
            CommentsListView.Items.Add(shareCountText);

            // Friend comments
            if (a.FriendComments.Count > 0)
            {
                TextBlock friendLabel = new TextBlock
                {
                    Text = a.FriendComments.Count == 1 ?
                        string.Format("{0} comment", a.FriendComments.Count) :
                        string.Format("{0} comments", a.FriendComments.Count),
                    Margin = new Thickness(0.0, 16.0, 0.0, 8.0),
                    Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
                };
                CommentsListView.Items.Add(friendLabel);

                foreach (Comment c in a.FriendComments)
                {
                    CommentBubble cb = new CommentBubble
                    {
                        DataContext = c
                    };
                    cb.LikeButton.Click += LikeButton_Click;
                    cb.EditButton.Click += EditButton_Click;
                    cb.ReplyButton.Click += ReplyButton_Click;
                    CommentsListView.Items.Add(cb);

                    foreach (Comment r in c.Replies)
                    {
                        ReplyBubble rb = new ReplyBubble
                        {
                            DataContext = r,
                            Tag = cb
                        };
                        rb.EditButton.Click += ReplyEditButton_Click;
                        CommentsListView.Items.Add(rb);
                    }
                }
            }

            // Friend shares
            if (a.FriendShares.Count > 0)
            {
                TextBlock friendShareLabel = new TextBlock
                {
                    Text = a.FriendShares.Count == 1 ?
                        string.Format("{0} share", a.FriendShares.Count) :
                        string.Format("{0} shares", a.FriendShares.Count),
                    Margin = new Thickness(0.0, 16.0, 0.0, 8.0),
                    Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
                };
                CommentsListView.Items.Add(friendShareLabel);

                foreach (Comment c in a.FriendShares)
                {
                    CommentBubble cb = new CommentBubble
                    {
                        DataContext = c
                    };
                    cb.LikeButton.Click += LikeButton_Click;
                    cb.EditButton.Click += EditButton_Click;
                    cb.ReplyButton.Click += ReplyButton_Click;
                    CommentsListView.Items.Add(cb);

                    foreach (Comment r in c.Replies)
                    {
                        ReplyBubble rb = new ReplyBubble
                        {
                            DataContext = r,
                            Tag = cb
                        };
                        rb.EditButton.Click += ReplyEditButton_Click;
                        CommentsListView.Items.Add(rb);
                    }
                }
            }

            // Public comments
            if (a.PublicComments.Count > 0)
            {
                TextBlock publicLabel = new TextBlock
                {
                    Text = a.PublicComments.Count == 1 ?
                        string.Format("{0} public comment", a.PublicComments.Count) :
                        string.Format("{0} public comments", a.PublicComments.Count),
                    Margin = new Thickness(0.0, 16.0, 0.0, 8.0),
                    Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
                };
                CommentsListView.Items.Add(publicLabel);

                foreach (Comment c in a.PublicComments)
                {
                    CommentBubble cb = new CommentBubble
                    {
                        DataContext = c
                    };
                    cb.LikeButton.Click += LikeButton_Click;
                    cb.EditButton.Click += EditButton_Click;
                    cb.ReplyButton.Click += ReplyButton_Click;
                    CommentsListView.Items.Add(cb);

                    foreach (Comment r in c.Replies)
                    {
                        ReplyBubble rb = new ReplyBubble
                        {
                            DataContext = r,
                            Tag = cb
                        };
                        rb.EditButton.Click += ReplyEditButton_Click;
                        CommentsListView.Items.Add(rb);
                    }
                }
            }

            // Public shares
            if (a.PublicShares.Count > 0)
            {
                TextBlock publicLabel = new TextBlock
                {
                    Text = a.PublicShares.Count == 1 ?
                        string.Format("{0} public share", a.PublicShares.Count) :
                        string.Format("{0} public shares", a.PublicShares.Count),
                    Margin = new Thickness(0.0, 16.0, 0.0, 8.0),
                    Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
                };
                CommentsListView.Items.Add(publicLabel);

                // Create a panel to hold the user icons for public shares
                VariableSizedWrapGrid wrapGrid = new VariableSizedWrapGrid
                {
                    MinWidth = 200,
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
                };
                foreach (int userId in a.PublicShares)
                {
                    BitmapImage image = new BitmapImage
                    {
                        DecodePixelWidth = 96,
                        UriSource = App.Client.Users.ContainsKey(userId) && App.Client.Users[userId].PhotoUri != null ?
                            App.Client.Users[userId].PhotoUri :
                            new Uri("ms-appx:///Assets/Square44x44Logo.png")
                    };

                    PersonPicture picture = new PersonPicture
                    {
                        Width = 32,
                        ProfilePicture = image,
                        Margin = new Thickness(1.0, 0.0, 1.0, 0.0)
                    };
                    if (App.Client.Users.ContainsKey(userId))
                    {
                        picture.DisplayName = App.Client.Users[userId].Username;

                        // Set the tooltip when you hover over the image
                        ToolTipService.SetToolTip(picture, picture.DisplayName);
                    }
                    wrapGrid.Children.Add(picture);
                }
                CommentsListView.Items.Add(wrapGrid);
            }

            // Posting UI
            Button shareButton = new Button
            {
                Content = a.IsShared ? "Story shared" : "Share this story",
                IsEnabled = !a.IsShared,
                Width = 140,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
            };
            shareButton.Click += ShareButton_Click;
            CommentsListView.Items.Add(shareButton);
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;

            RemoveActiveTextBoxes();

            int buttonIndex = CommentsListView.Items.IndexOf(button);
            if (buttonIndex == -1) return;

            // Replace the share button with a comment text box.
            CommentsListView.Items.RemoveAt(buttonIndex);

            activeCommentTextBox = new CommentTextBox
            {
                DataContext = App.Client.LoggedInUser,
                Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
            };
            activeCommentTextBox.DeleteButton.Visibility = Visibility.Collapsed;
            activeCommentTextBox.CancelButton.Click += ShareCancelButton_Click;
            activeCommentTextBox.SubmitButton.Click += ShareSubmitButton_Click;
            activeCommentTextBox.CommentsBox.KeyUp += CommentsBox_KeyUp;
            CommentsListView.Items.Insert(buttonIndex, activeCommentTextBox);
            CommentsListView.ScrollIntoView(activeCommentTextBox);

            activeCommentTextBox.Focus(FocusState.Programmatic);
        }

        private void ShareCancelButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveActiveTextBoxes();
        }

        private async void ShareSubmitButton_Click(object sender, RoutedEventArgs args)
        {
            if (activeCommentTextBox == null) return;

            // Disable buttons to prevent duplicate requests
            activeCommentTextBox.SubmitButton.IsEnabled = false;
            activeCommentTextBox.CancelButton.IsEnabled = false;
            activeCommentTextBox.DeleteButton.IsEnabled = false;
            activeCommentTextBox.SubmitProgress.Visibility = Visibility.Visible;

            try
            {
                Article newArticle = await viewModel.ShareArticleAsync(activeCommentTextBox.CommentsBox.Text);
                LoadComments(newArticle);
            }
            catch (Exception e)
            {
                activeCommentTextBox.SubmitButton.IsEnabled = true;
                activeCommentTextBox.CancelButton.IsEnabled = true;
                activeCommentTextBox.DeleteButton.IsEnabled = true;
                activeCommentTextBox.SubmitProgress.Visibility = Visibility.Collapsed;
                DialogHelper.DisplayErrorMessage("Failed to share story", e.Message);
            }
        }

        private async void ShareDeleteButton_Click(object sender, RoutedEventArgs args)
        {
            if (!(sender is Button button)) return;
            if (!(button.Tag is CommentTextBox textBox)) return;

            // Disable buttons to prevent duplicate requests
            textBox.SubmitButton.IsEnabled = false;
            textBox.CancelButton.IsEnabled = false;
            textBox.DeleteButton.IsEnabled = false;
            textBox.SubmitProgress.Visibility = Visibility.Visible;

            try
            {
                Article newArticle = await viewModel.UnshareArticleAsync();
                LoadComments(newArticle);
            }
            catch (Exception e)
            {
                textBox.SubmitButton.IsEnabled = true;
                textBox.CancelButton.IsEnabled = true;
                textBox.DeleteButton.IsEnabled = true;
                textBox.SubmitProgress.Visibility = Visibility.Collapsed;
                DialogHelper.DisplayErrorMessage("Failed to unshare story", e.Message);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.Tag is CommentBubble bubble)) return;
            if (!(bubble.DataContext is Comment sourceComment)) return;

            RemoveActiveTextBoxes();

            int commentIndex = CommentsListView.Items.IndexOf(bubble);
            if (commentIndex == -1) return;

            // Replace the comment bubble with a comment textbox at the same location.
            CommentsListView.Items.RemoveAt(commentIndex);

            activeCommentTextBox = new CommentTextBox
            {
                DataContext = App.Client.LoggedInUser,
                SourceComment = sourceComment,
                SourceCommentBubble = bubble
            };
            activeCommentTextBox.CommentsBox.Text = sourceComment.CommentString;
            activeCommentTextBox.CommentsBox.KeyUp += CommentsBox_KeyUp;
            activeCommentTextBox.CancelButton.Click += EditCancelButton_Click;
            activeCommentTextBox.DeleteButton.Click += ShareDeleteButton_Click;
            activeCommentTextBox.SubmitButton.Click += ShareSubmitButton_Click;

            CommentsListView.Items.Insert(commentIndex, activeCommentTextBox);
            CommentsListView.ScrollIntoView(activeCommentTextBox);

            activeCommentTextBox.Focus(FocusState.Programmatic);
        }

        private void EditCancelButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveActiveTextBoxes();
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs args)
        {
            if (!(sender is Button button)) return;
            if (!(button.DataContext is Comment comment)) return;
            if (viewModel.SelectedArticle == null) return;

            // Disable button to prevent duplicate requests
            button.IsEnabled = false;

            try
            {
                await viewModel.ToggleLikeAsync(comment);
            }
            catch (Exception e)
            {
                DialogHelper.DisplayErrorMessage("Failed to like comment", e.Message);
            }

            // Re-add event handler now that the request is completed
            button.IsEnabled = true;
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.DataContext is Comment comment)) return;
            if (!(button.Tag is CommentBubble bubble)) return;

            // Remove existing reply text box if present.
            RemoveActiveTextBoxes();

            // Calculate the index where we should create the reply textbox.
            int commentIndex = CommentsListView.Items.IndexOf(bubble);
            if (commentIndex == -1) return;

            int replyIndex = commentIndex + comment.Replies.Count + 1;

            activeReplyTextBox = new ReplyTextBox
            {
                DataContext = App.Client.LoggedInUser,
                SourceComment = comment,
                SourceCommentBubble = bubble
            };
            activeReplyTextBox.CancelButton.Click += ReplyCancelButton_Click;
            activeReplyTextBox.SubmitButton.Click += ReplySubmitButton_Click;
            activeReplyTextBox.ReplyContentsBox.KeyUp += ReplyBox_KeyUp;

            CommentsListView.Items.Insert(replyIndex, activeReplyTextBox);
            CommentsListView.ScrollIntoView(activeReplyTextBox);

            activeReplyTextBox.Focus(FocusState.Programmatic);
        }

        private void ReplyCancelButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveActiveTextBoxes();
        }

        private async void ReplySubmitButton_Click(object sender, RoutedEventArgs args)
        {
            if (viewModel.SelectedArticle == null) return;
            if (activeReplyTextBox == null) return;
            if (string.IsNullOrEmpty(activeReplyTextBox.ReplyContentsBox.Text)) return;

            // Disable buttons to prevent duplicate requests
            activeReplyTextBox.SubmitButton.IsEnabled = false;
            activeReplyTextBox.CancelButton.IsEnabled = false;
            activeReplyTextBox.DeleteButton.IsEnabled = false;
            activeReplyTextBox.SubmitProgress.Visibility = Visibility.Visible;

            try
            {
                Comment c = await viewModel.ReplyToCommentAsync(
                    activeReplyTextBox.ReplyContentsBox.Text, activeReplyTextBox.SourceComment, activeReplyTextBox.OriginalReply);
                ReloadComment(c, activeReplyTextBox);
            }
            catch (Exception e)
            {
                activeReplyTextBox.SubmitButton.IsEnabled = true;
                activeReplyTextBox.CancelButton.IsEnabled = true;
                activeReplyTextBox.DeleteButton.IsEnabled = true;
                activeReplyTextBox.SubmitProgress.Visibility = Visibility.Collapsed;
                DialogHelper.DisplayErrorMessage("Failed to submit reply", e.Message);
            }
        }

        private void ReplyEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.Tag is ReplyBubble replyBubble)) return;
            if (!(replyBubble.DataContext is Comment replyComment)) return;
            if (!(replyBubble.Tag is CommentBubble sourceCommentBubble)) return;
            if (!(sourceCommentBubble.DataContext is Comment sourceComment)) return;

            RemoveActiveTextBoxes();

            int replyIndex = CommentsListView.Items.IndexOf(replyBubble);
            if (replyIndex == -1) return;

            // Replace the reply bubble with a reply textbox at the same location.
            CommentsListView.Items.RemoveAt(replyIndex);

            activeReplyTextBox = new ReplyTextBox
            {
                DataContext = App.Client.LoggedInUser,
                SourceComment = sourceComment,
                SourceCommentBubble = sourceCommentBubble,
                OriginalReply = replyComment
            };
            activeReplyTextBox.ReplyContentsBox.Text = replyComment.CommentString;
            activeReplyTextBox.ReplyContentsBox.KeyUp += ReplyBox_KeyUp;
            activeReplyTextBox.DeleteButton.Visibility = Visibility.Visible;
            activeReplyTextBox.DeleteButton.Click += ReplyEditDeleteButton_Click;
            activeReplyTextBox.CancelButton.Click += ReplyEditCancelButton_Click;
            activeReplyTextBox.SubmitButton.Click += ReplySubmitButton_Click;

            CommentsListView.Items.Insert(replyIndex, activeReplyTextBox);
            CommentsListView.ScrollIntoView(activeReplyTextBox);

            activeReplyTextBox.Focus(FocusState.Programmatic);
        }

        private async void ReplyEditDeleteButton_Click(object sender, RoutedEventArgs args)
        {
            if (!(sender is Button button)) return;
            if (!(button.Tag is ReplyTextBox textBox)) return;
            if (viewModel.SelectedArticle == null) return;

            // Disable buttons to prevent duplicate requests
            textBox.SubmitButton.IsEnabled = false;
            textBox.CancelButton.IsEnabled = false;
            textBox.DeleteButton.IsEnabled = false;
            textBox.SubmitProgress.Visibility = Visibility.Visible;

            try
            {
                Comment c = await viewModel.DeleteReplyAsync(textBox.SourceComment, textBox.OriginalReply);
                ReloadComment(c, textBox);
            }
            catch (Exception e)
            {
                textBox.SubmitButton.IsEnabled = true;
                textBox.CancelButton.IsEnabled = true;
                textBox.DeleteButton.IsEnabled = true;
                textBox.SubmitProgress.Visibility = Visibility.Collapsed;
                DialogHelper.DisplayErrorMessage("Failed to delete reply", e.Message);
            }
        }

        private void ReplyEditCancelButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveActiveTextBoxes();
        }

        private void ReloadComment(Comment comment, ReplyTextBox textBox)
        {
            // Remove the reply text box.
            RemoveActiveTextBoxes();

            int sourceIndex = CommentsListView.Items.IndexOf(textBox.SourceCommentBubble);
            if (sourceIndex == -1) return;

            // Update UI, replacing the text box with a submitted comment
            // The number of UI elements to remove is equal to the number of replies,
            // plus one for the original comment.
            int numElementsToRemove = textBox.SourceComment.Replies.Count + 1;
            for (int i = 0; i < numElementsToRemove; i++)
            {
                CommentsListView.Items.RemoveAt(sourceIndex);
            }

            CommentBubble cb = new CommentBubble
            {
                DataContext = comment
            };
            cb.LikeButton.Click += LikeButton_Click;
            cb.EditButton.Click += EditButton_Click;
            cb.ReplyButton.Click += ReplyButton_Click;

            int idx = sourceIndex;

            CommentsListView.Items.Insert(idx, cb);
            idx++;

            foreach (Comment r in comment.Replies)
            {
                ReplyBubble rb = new ReplyBubble
                {
                    DataContext = r,
                    Tag = cb
                };
                rb.EditButton.Click += ReplyEditButton_Click;
                CommentsListView.Items.Insert(idx, rb);
                idx++;
            }
        }

        private void RemoveActiveTextBoxes()
        {
            // Remove active reply text box
            if (activeReplyTextBox != null)
            {
                int replyIndex = CommentsListView.Items.IndexOf(activeReplyTextBox);
                if (replyIndex == -1) return;

                CommentsListView.Items.RemoveAt(replyIndex);

                if (activeReplyTextBox.OriginalReply != null)
                {
                    // This was a reply edit. Replace the removed text box with the original reply bubble.
                    ReplyBubble replyBubble = new ReplyBubble
                    {
                        DataContext = activeReplyTextBox.OriginalReply,
                        Tag = activeReplyTextBox.SourceCommentBubble
                    };
                    replyBubble.EditButton.Click += ReplyEditButton_Click;
                    CommentsListView.Items.Insert(replyIndex, replyBubble);
                }
                else
                {
                    // This was a new reply. Add back click event handler to allow new replies
                    activeReplyTextBox.SourceCommentBubble.ReplyButton.Click += ReplyButton_Click;
                }

                activeReplyTextBox = null;
            }

            // Remove active comment text box
            if (activeCommentTextBox != null)
            {
                int commentIndex = CommentsListView.Items.IndexOf(activeCommentTextBox);
                if (commentIndex == -1) return;

                CommentsListView.Items.RemoveAt(commentIndex);

                if (activeCommentTextBox.SourceCommentBubble != null)
                {
                    // This was an share edit. Replace the text box with the original comment bubble.
                    CommentsListView.Items.Insert(commentIndex, activeCommentTextBox.SourceCommentBubble);
                }
                else
                {
                    // This was an original share. Replace the text box with the share button.
                    Button shareButton = new Button
                    {
                        Content = "Share this story",
                        Width = 140,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
                    };
                    shareButton.Click += ShareButton_Click;
                    CommentsListView.Items.Insert(commentIndex, shareButton);
                }

                activeCommentTextBox = null;
            }
        }

        private void CommentsBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (activeCommentTextBox == null) return;

            if (e.Key == VirtualKey.Enter)
            {
                ShareSubmitButton_Click(sender, e);
            }
        }

        private void ReplyBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (activeReplyTextBox == null) return;

            if (e.Key == VirtualKey.Enter)
            {
                ReplySubmitButton_Click(sender, e);
            }
        }

        #endregion

        #region Reading Style

        private void ReadingTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is ReadingThemeViewModel option)
            {
                if (option != selectedReadingTheme)
                {
                    selectedReadingTheme = option;
                    UpdateReadingTheme();
                }
            }
        }

        private void UpdateReadingTheme()
        {
            App.Settings.ReadingTheme = selectedReadingTheme.ThemeMode;

            ArticleContent.RequestedTheme = selectedReadingTheme.RequestedTheme;
            ArticleContent.Background = new SolidColorBrush(
                ReadingThemeViewModel.GetWebViewBackgroundColorForReadingTheme(selectedReadingTheme.ThemeMode));

            OnReadingThemeChanged();
        }

        private void Settings_ThemeChanged(object sender, EventArgs e)
        {
            ArticleContent.RequestedTheme = selectedReadingTheme.RequestedTheme;
            ArticleContent.Background = new SolidColorBrush(
                ReadingThemeViewModel.GetWebViewBackgroundColorForReadingTheme(selectedReadingTheme.ThemeMode));
            OnReadingThemeChanged();
        }

        private void OnReadingThemeChanged()
        {
            if (viewModel != null)
            {
                // Walk through all articles updating their ViewContent to match the new theme.
                viewModel.RefreshArticleContent();

                // Reload comments so hopefully the PersonPicture controls work properly
                LoadComments(viewModel.SelectedArticle);
            }
        }

        #endregion
    }
}
