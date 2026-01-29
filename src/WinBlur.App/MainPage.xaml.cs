using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.ViewModel;
using Windows.System;

namespace WinBlur.App
{
    /// <summary>
    /// This class describes the logic for the MainPage.
    /// </summary>
    public partial class MainPage : Page
    {
        #region Fields

        private MainViewModel viewModel;
        private bool isNewPageInstance;
        private Flyout activeFlyout;

        #endregion Fields

        #region Initialization

        /// <summary>
        /// Constructor for MainPage.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;

            isNewPageInstance = true;

            App.Client.FeedMarkedAsRead += FeedMarkedAsRead;
            MainViewModel.Instance.NextSiteAcceleratorInvoked += MainViewModel_NextSiteAcceleratorInvoked;
            MainViewModel.Instance.PreviousSiteAcceleratorInvoked += MainViewModel_PreviousSiteAcceleratorInvoked;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.Settings.ShouldShowUpgradeDialog)
            {
                try
                {
                    await UpgradeDialog.ShowAsync();
                    App.Settings.ShouldShowUpgradeDialog = false;
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion Initialization

        #region Navigation

        /// <summary>
        /// Event handler for the OnNavigatedTo method.
        /// This event occurs every time the page is navigated to.
        /// If the backend doesn't have access tokens, we need to ask the user to log in.
        /// </summary>
        /// <param name="e">Event parameters</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((string)e.Parameter == "login")
            {
                isNewPageInstance = true;
            }

            if (isNewPageInstance)
            {
                BackgroundTaskManager.RegisterBackgroundTask();

                viewModel = MainViewModel.Instance;
                DataContext = viewModel;
            }
            isNewPageInstance = false;

            viewModel.SyncUnreadCountTimer.Start();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            viewModel.SyncUnreadCountTimer.Stop();
            base.OnNavigatedFrom(e);
        }

        #endregion

        #region Syncing

        private async void syncSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            await SyncSubscriptionsAsync();
        }

        private async void splitView_Loaded(object sender, RoutedEventArgs e)
        {
            await SyncSubscriptionsAsync();
        }

        private async void TreeViewRefresh_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            await SyncSubscriptionsAsync();
        }

        private async Task SyncSubscriptionsAsync()
        {
            UpdateFeedModeFlyout(App.Settings.FeedFilterMode);
            await viewModel.SyncSubscriptionListAsync();
        }

        #endregion Syncing

        #region Feed List

        private void SplitViewToggleButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void TreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Item is SubscriptionLabel label)
            {
                viewModel.SaveCompressedStateForLabel(label);
            }
        }

        private void TreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            if (args.Item is SubscriptionLabel label)
            {
                viewModel.SaveCompressedStateForLabel(label);
            }
        }

        private void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0 && args.AddedItems[0] is SubscriptionLabel label)
            {
                SelectSubscription(label);
            }
        }

        private void SelectSubscription(SubscriptionLabel label)
        {
            // Only handle the subscription change if the selection is different from
            // what we already have. This can happen if the TreeView forgets the selection
            // during a filter operation and we need to re-select it.
            if (viewModel.SelectedSubscription != label)
            {
                if (splitView.DisplayMode == SplitViewDisplayMode.Overlay)
                {
                    // Hide the pane if we're in the overlay mode
                    // since we've successfully chosen a thing
                    splitView.IsPaneOpen = false;
                }

                viewModel.SelectedSubscription = label;
                ContentFrame.Navigate(typeof(FeedPage), label);
            }
        }

        private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Get the ListViewItem that was tapped
            if (sender is TreeViewItem item)
            {
                // Don't show the context menu for feeds with no parent folder
                // Also, hide the context menu for social friends, because this isn't enabled yet.
                // TODO: enable this!
                if (item.DataContext is SubscriptionLabel label && label.ParentFolder != null && label.Type == SubscriptionType.Site)
                {
                    // Make a MenuFlyout
                    MenuFlyout flyout = new MenuFlyout();
                    flyout.Placement = FlyoutPlacementMode.Bottom;

                    // Create move menu item, with a submenu to choose where to move it to
                    MenuFlyoutSubItem moveItem = new MenuFlyoutSubItem()
                    {
                        Icon = new SymbolIcon(Symbol.MoveToFolder),
                        Text = "Move to",
                        Tag = item,
                    };

                    foreach (FolderLabel folder in viewModel.FolderList)
                    {
                        MenuFlyoutItem folderItem = new MenuFlyoutItem
                        {
                            Icon = new FontIcon
                            {
                                Glyph = Converters.IconGlyphToString(Converters.IconGlyph.Folder)
                            },
                            Text = folder.Title,
                            Tag = item,
                            MinWidth = 100,
                            Margin = folder.DepthToMargin(0)
                        };

                        if (label.IsFolder)
                        {
                            folderItem.Click += moveFolder_Click;
                        }
                        else
                        {
                            folderItem.Click += moveSite_Click;
                        }

                        moveItem.Items.Add(folderItem);
                    }

                    flyout.Items.Add(moveItem);

                    MenuFlyoutItem renameItem = new MenuFlyoutItem()
                    {
                        Icon = new SymbolIcon(Symbol.Rename),
                        Text = "Rename",
                        Tag = item,
                    };
                    renameItem.Click += renameSubscription_Click;
                    flyout.Items.Add(renameItem);

                    MenuFlyoutItem deleteItem = new MenuFlyoutItem()
                    {
                        Icon = new SymbolIcon(Symbol.Delete),
                        Text = "Delete",
                        Tag = item,
                    };
                    deleteItem.Click += deleteSubscription_Click;
                    flyout.Items.Add(deleteItem);

                    flyout.ShowAt(item);
                }
            }
        }

        private void feedModeSplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            sender.Flyout.ShowAt(sender);
        }

        private void UpdateFeedModeFlyout(FeedMode mode)
        {
            foreach (var item in feedModeMenuFlyout.Items)
            {
                var radioFlyoutItem = (RadioMenuFlyoutItem)item;
                radioFlyoutItem.IsChecked = (FeedMode)item.DataContext == mode;
                if (radioFlyoutItem.IsChecked)
                {
                    feedModeSplitButton.Content = radioFlyoutItem.Text;
                }
            }
        }

        private void FeedModeFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (TreeView == null || viewModel == null) return;

            var item = (RadioMenuFlyoutItem)sender;
            if (item.IsChecked)
            {
                viewModel.FeedMode = (FeedMode)item.DataContext;
            }
        }

        private void MarkAllAsRead_Click(object sender, RoutedEventArgs e)
        {
            App.Window.ShowMarkAllAsReadDialog();
        }

        private async void KeyboardShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            await KeyboardShortcutsDialog.ShowAsync();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // Clear selected item in Tree View so state is consistent.
            TreeView.SelectedItem = null;
            ContentFrame.Navigate(typeof(SettingsPage), null);
        }

        private void FeedMarkedAsRead(object sender, EventArgs e)
        {
            if (viewModel.SelectedSubscription is SubscriptionLabel label)
            {
                SubscriptionLabel nextLabel = null;
                if (label.IsFolder)
                {
                    // This only works if a site folder is selected, and it's not the root (i.e. not "All Site Stories")
                    if (label.Type == SubscriptionType.Site && label.ParentLabel != null)
                    {
                        nextLabel = FindNextFolder(label);
                    }
                }
                else
                {
                    // This works across social and sites, but not saved story tags
                    if (label.Type == SubscriptionType.Site || label.Type == SubscriptionType.Social)
                    {
                        nextLabel = FindNextSite(label, true);
                    }
                }

                if (nextLabel != null)
                {
                    SelectSubscription(nextLabel);
                }
            }
        }

        private void MainViewModel_NextSiteAcceleratorInvoked(object sender, EventArgs e)
        {
            NextSiteKeyboardAccelerator_Invoked(null, null);
        }

        private void MainViewModel_PreviousSiteAcceleratorInvoked(object sender, EventArgs e)
        {
            PreviousSiteKeyboardAccelerator_Invoked(null, null);
        }

        private void NextSiteKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SubscriptionLabel nextLabel = null;
            if (viewModel.SelectedSubscription is SubscriptionLabel label)
            {
                if (label.IsFolder)
                {
                    // This only works if a site folder is selected
                    if (label.Type == SubscriptionType.Site)
                    {
                        nextLabel = FindNextFolder(label, true);
                    }
                }
                else
                {
                    // This works across social and sites, but not saved story tags
                    if (label.Type == SubscriptionType.Site || label.Type == SubscriptionType.Social)
                    {
                        nextLabel = FindNextSite(label, true);
                    }
                }
            }
            else
            {
                // Nothing selected - select first site in the list.
                nextLabel = FindNextSite(viewModel.FilteredSubscriptions[0], false);
            }

            if (nextLabel != null)
            {
                SelectSubscription(nextLabel);
            }

            if (args != null)
            {
                args.Handled = true;
            }
        }

        private void PreviousSiteKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SubscriptionLabel prevLabel = null;
            if (viewModel.SelectedSubscription is SubscriptionLabel label)
            {
                if (label.IsFolder)
                {
                    // This only works if a site folder is selected
                    if (label.Type == SubscriptionType.Site)
                    {
                        prevLabel = FindPrevFolder(label, true);
                    }
                }
                else
                {
                    // This works across social and sites, but not saved story tags
                    if (label.Type == SubscriptionType.Site || label.Type == SubscriptionType.Social)
                    {
                        prevLabel = FindPrevSite(label, true);
                    }
                }
            }
            else
            {
                // Nothing selected - select last site in the list.
                prevLabel = FindPrevSite(viewModel.FilteredSubscriptions.Last(), false);
            }

            if (prevLabel != null)
            {
                SelectSubscription(prevLabel);
            }

            if (args != null)
            {
                args.Handled = true;
            }
        }

        /// <summary>
        /// Finds the next site folder in the TreeView.
        /// </summary>
        /// <param name="label">Label to start at</param>
        /// <returns>Next folder to show</returns>
        private SubscriptionLabel FindNextFolder(SubscriptionLabel label, bool allowWrapAround = false)
        {
            if (!allowWrapAround && label.ParentLabel == null)
            {
                // This means we wrapped around and hit "All Site Stories". Just return that
                return label;
            }

            IList<SubscriptionLabel> parentList = label.ParentLabel?.FilteredChildren;
            if (parentList == null || parentList.Count == 0)
            {
                // This is the special case where the first level of folders/sites are at the same tree depth as "All Site Stories".
                parentList = viewModel.FilteredSubscriptions;
            }

            // Current folder was marked as read, so start at the parent label.
            int indexInParent = parentList.IndexOf(label);
            if (indexInParent == -1)
            {
                // This shouldn't happen.
                return null;
            }

            // Starting at the very next subscription, linear search for the next applicable folder.
            // Yes it's not efficient :(
            for (int i = indexInParent + 1; i < parentList.Count; i++)
            {
                SubscriptionLabel l = parentList[i];
                if (l.Type != SubscriptionType.Site)
                {
                    // We went through the entire site list. Break early to avoid going through Saved stories
                    break;
                }

                if (l.IsUnderCompressedFolder)
                {
                    // Skip entries that are not visible in the list.
                    break;
                }

                if (l.IsFolder)
                {
                    return l;
                }
            }

            // We got to the end of the parent list. Try going up a level
            return FindNextFolder(label.ParentLabel);
        }

        private SubscriptionLabel FindPrevFolder(SubscriptionLabel label, bool allowWrapAround = false)
        {
            if (!allowWrapAround && label.ParentLabel == null)
            {
                // This means we wrapped around and hit "All Site Stories". Just return that
                return label;
            }

            IList<SubscriptionLabel> parentList = label.ParentLabel?.FilteredChildren;
            if (parentList == null || parentList.Count == 0)
            {
                // This is the special case where the first level of folders/sites are at the same tree depth as "All Site Stories".
                parentList = viewModel.FilteredSubscriptions;
            }

            // Current folder was marked as read, so start at the parent label.
            int indexInParent = parentList.IndexOf(label);
            if (indexInParent == -1)
            {
                // This shouldn't happen.
                return null;
            }

            // Starting at the very next subscription, linear search for the next applicable folder.
            // Yes it's not efficient :(
            for (int i = indexInParent - 1; i >= 0; i--)
            {
                SubscriptionLabel l = parentList[i];
                if (l.Type != SubscriptionType.Site)
                {
                    // We went through the entire site list. Break early to avoid going through Saved stories
                    break;
                }

                if (l.IsUnderCompressedFolder)
                {
                    // Skip entries that are not visible in the list.
                    break;
                }

                if (l.IsFolder)
                {
                    return l;
                }
            }

            // We got to the end of the parent list. Try going up a level
            return FindPrevFolder(label.ParentLabel, allowWrapAround);
        }

        private SubscriptionLabel FindNextSite(SubscriptionLabel label, bool skipCurrentItem)
        {
            if (label == null)
            {
                // This is the case where searching bubbled up to the root. Wrap around to the beginning.
                label = viewModel.FilteredSubscriptions.First();
            }

            IList<SubscriptionLabel> parentList = label.ParentLabel?.FilteredChildren;
            if (parentList == null || parentList.Count == 0)
            {
                // This is the special case where the first level of folders/sites are at the same tree depth as "All Site Stories".
                parentList = viewModel.FilteredSubscriptions;
            }

            // Current folder was marked as read, so start at the parent label.
            int indexInParent = parentList.IndexOf(label);
            if (indexInParent == -1)
            {
                // This shouldn't happen.
                return null;
            }

            for (int i = skipCurrentItem ? indexInParent + 1 : indexInParent; i < parentList.Count; i++)
            {
                SubscriptionLabel l = parentList[i];
                if (l.Type == SubscriptionType.Saved)
                {
                    // We went through the entire site list. Break early to avoid going through Saved stories
                    break;
                }

                if (l.IsUnderCompressedFolder)
                {
                    // Skip entries that are not visible in the list.
                    break;
                }

                if (!l.IsFolder)
                {
                    return l;
                }
                else if (l.FilteredChildren.Count != 0 && !l.IsCompressed)
                {
                    // Drill into the folder to find the first site inside. Skip folders that are collapsed.
                    // Pass false so we don't skip the first item in the folder.
                    SubscriptionLabel nextLabelInFolder = FindNextSite(l.FilteredChildren.First(), false);
                    if (nextLabelInFolder != null)
                    {
                        return nextLabelInFolder;
                    }
                }
            }

            // We reached the end of the list. Try going up a level
            return FindNextSite(label.ParentLabel, true);
        }

        private SubscriptionLabel FindPrevSite(SubscriptionLabel label, bool skipCurrentItem)
        {
            if (label == null)
            {
                // This is the case where searching bubbled up to the root. Wrap around to the end.
                label = viewModel.FilteredSubscriptions.Last();
            }

            IList<SubscriptionLabel> parentList = label.ParentLabel?.FilteredChildren;
            if (parentList == null || parentList.Count == 0)
            {
                // This is the special case where the first level of folders/sites are at the same tree depth as "All Site Stories".
                parentList = viewModel.FilteredSubscriptions;
            }

            // Current folder was marked as read, so start at the parent label.
            int indexInParent = parentList.IndexOf(label);
            if (indexInParent == -1)
            {
                // This shouldn't happen.
                return null;
            }

            for (int i = skipCurrentItem ? indexInParent - 1 : indexInParent; i >= 0; i--)
            {
                SubscriptionLabel l = parentList[i];
                if (l.Type == SubscriptionType.Saved)
                {
                    // We went through the entire site list. Break early to avoid going through Saved stories
                    continue;
                }

                if (l.IsUnderCompressedFolder)
                {
                    // Skip entries that are not visible in the list.
                    break;
                }

                if (!l.IsFolder)
                {
                    return l;
                }
                else if (l.FilteredChildren.Count != 0 && !l.IsCompressed)
                {
                    // Drill into the folder to find the last site inside. Skip folders that are collapsed.
                    // Pass false so we don't skip the last item in the folder.
                    SubscriptionLabel nextLabelInFolder = FindPrevSite(l.FilteredChildren.Last(), false);
                    if (nextLabelInFolder != null)
                    {
                        return nextLabelInFolder;
                    }
                }
            }

            // We reached the end of the list. Try going up a level
            return FindPrevSite(label.ParentLabel, true);
        }

        #endregion

        #region Feed Management

        private void AddSite_Click(object sender, RoutedEventArgs e)
        {
            App.Window.ShowAddSiteDialog();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            App.Window.ShowAddFolderDialog();
        }

        private async void deleteSubscription_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                if (item.DataContext is SubscriptionLabel label)
                {
                    // Set dialog state
                    DeleteLoadingPanel.Visibility = Visibility.Collapsed;
                    DeleteErrorText.Visibility = Visibility.Collapsed;
                    DeleteDialog.Tag = label;

                    if (label.IsFolder)
                    {
                        DeleteDialog.Title = "Delete folder permanently?";
                        DeleteText.Text = string.Format("The folder {0} and all {1} feeds inside it will be deleted forever.",
                            label.Title, label.Subitems.Count);
                    }
                    else
                    {
                        DeleteDialog.Title = "Delete site permanently?";
                        DeleteText.Text = string.Format("The site {0} will be deleted forever.", label.Title);
                    }

                    try
                    {
                        await DeleteDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private async void DeleteDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (sender.Tag is SubscriptionLabel label)
            {
                var deferral = args.GetDeferral();
                DeleteLoadingPanel.Visibility = Visibility.Visible;

                try
                {
                    await viewModel.DeleteItemAsync(label);
                }
                catch (Exception e)
                {
                    args.Cancel = true;
                    DeleteLoadingPanel.Visibility = Visibility.Collapsed;
                    DeleteErrorText.Text = string.Format("Failed to delete item: {0}", e.Message);
                    DeleteErrorText.Visibility = Visibility.Visible;
                }

                DeleteLoadingPanel.Visibility = Visibility.Collapsed;
                deferral.Complete();
            }
        }

        private void renameSubscription_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                if (item.DataContext is SubscriptionLabel label)
                {
                    if (item.Tag is TreeViewItem viewItem)
                    {
                        // Make flyout and show it below the item
                        activeFlyout = new Flyout();
                        activeFlyout.Placement = FlyoutPlacementMode.Bottom;

                        StackPanel panel = new StackPanel();
                        panel.Width = 250;

                        TextBox box = new TextBox();
                        box.PlaceholderText = label.IsFolder ? "Folder name" : "Site name";
                        box.Text = label.Title;
                        box.SelectAll();
                        box.Margin = new Thickness(0, 0, 0, 10);
                        box.KeyUp += renameBox_KeyUp;
                        panel.Children.Add(box);

                        Button button = new Button();
                        button.Content = "Rename";
                        button.Click += RenameSubscription;
                        panel.Children.Add(button);

                        button.Tag = new Tuple<SubscriptionLabel, TextBox>(label, box);
                        box.Tag = button;

                        activeFlyout.Content = panel;
                        activeFlyout.ShowAt(viewItem);
                    }
                }
            }
        }

        private void renameBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (e.Key == VirtualKey.Enter)
                {
                    // Submit rename
                    RenameSubscription(box.Tag, null);
                    e.Handled = true;
                }
            }
        }

        private async void RenameSubscription(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                if (button.Tag is Tuple<SubscriptionLabel, TextBox> tag)
                {
                    if (activeFlyout != null)
                    {
                        activeFlyout.Hide();
                    }

                    SubscriptionLabel label = tag.Item1;
                    TextBox box = tag.Item2;

                    if (box.Text == "")
                    {
                        return;
                    }

                    viewModel.IsLoading = true;

                    try
                    {
                        await viewModel.RenameItemAsync(label, box.Text);
                    }
                    catch (Exception e)
                    {
                        DialogHelper.DisplayErrorMessage("Failed to rename item", e.Message);
                    }

                    viewModel.IsLoading = false;
                }
            }
        }

        private async void moveFolder_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuFlyoutItem item)
            {
                if (item.DataContext is SubscriptionLabel label)
                {
                    string folder = label.Title;
                    string fromFolder = label.ParentFolder;
                    string toFolder = item.Text;

                    // First do error checking
                    // You can't move a folder into itself or its parent
                    if (toFolder == folder || toFolder == fromFolder)
                    {
                        return;
                    }

                    viewModel.IsLoading = true;

                    try
                    {
                        await viewModel.MoveItemAsync(label, fromFolder, toFolder);
                    }
                    catch (Exception e)
                    {
                        DialogHelper.DisplayErrorMessage("Failed to move folder", e.Message);
                    }

                    viewModel.IsLoading = false;
                }
            }
        }

        private async void moveSite_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuFlyoutItem item)
            {
                if (item.DataContext is SubscriptionLabel label)
                {
                    viewModel.IsLoading = true;

                    try
                    {
                        await viewModel.MoveItemAsync(label, label.ParentFolder, item.Text);
                    }
                    catch (Exception e)
                    {
                        DialogHelper.DisplayErrorMessage("Failed to move site", e.Message);
                    }

                    viewModel.IsLoading = false;
                }
            }
        }

        #endregion

        #region Test Mode

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.ToggleTheme();
        }

        #endregion
    }
}