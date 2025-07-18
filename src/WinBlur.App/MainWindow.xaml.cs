using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.ViewModel;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()

namespace WinBlur.App
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public Frame RootFrame { get { return PageFrame; } }
        public TestModeHelper TestModeHelper { get { return App.TestModeHelper; } }

        private DispatcherTimer siteAutoCompleteTimer;

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            PersistenceId = "WinBlur.App.MainWindow";

            Activated += MainWindow_Activated;
            Title = AppTitleText.Text;

            siteAutoCompleteTimer = new DispatcherTimer();
            siteAutoCompleteTimer.Tick += SiteAutoCompleteTimer_Tick;
            siteAutoCompleteTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                AppTitleText.Foreground = (SolidColorBrush)Application.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                AppTitleText.Foreground = (SolidColorBrush)Application.Current.Resources["WindowCaptionForeground"];
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        public async void ShowAddSiteDialog()
        {
            // Reset dialog state
            AddSiteBox.Text = "";
            AddSiteFolderPicker.SelectedIndex = -1;
            AddSiteLoadingPanel.Visibility = Visibility.Collapsed;
            AddSiteErrorText.Visibility = Visibility.Collapsed;

            try
            {
                await AddSiteDialog.ShowAsync();
            }
            catch (Exception)
            {
            }
        }

        private async void AddSiteDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AddSiteErrorText.Visibility = Visibility.Collapsed;

            string url = AddSiteBox.Text;
            if (url == null || url == "" || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                args.Cancel = true;
                AddSiteErrorText.Text = "Make sure a URL is filled in.";
                AddSiteErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (!(AddSiteFolderPicker.SelectedItem is FolderLabel label))
            {
                args.Cancel = true;
                AddSiteErrorText.Text = "Make sure a parent folder is selected.";
                AddSiteErrorText.Visibility = Visibility.Visible;
                return;
            }

            string folder = label.Title;
            if (folder == null || folder == "")
            {
                args.Cancel = true;
                AddSiteErrorText.Text = "Make sure a parent folder is selected.";
                AddSiteErrorText.Visibility = Visibility.Visible;
                return;
            }
            else if (folder == "Top Level")
            {
                folder = "";
            }

            var deferral = args.GetDeferral();
            AddSiteLoadingPanel.Visibility = Visibility.Visible;

            try
            {
                await MainViewModel.Instance.AddSiteAsync(url, folder);
            }
            catch (Exception e)
            {
                args.Cancel = true;
                AddSiteLoadingPanel.Visibility = Visibility.Collapsed;
                AddSiteErrorText.Text = string.Format("Failed to add site: {0}", e.Message);
                AddSiteErrorText.Visibility = Visibility.Visible;
            }

            AddSiteLoadingPanel.Visibility = Visibility.Collapsed;
            deferral.Complete();
        }

        private void AddSiteBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
            {
                MainViewModel.Instance.SiteAutoCompleteList.Clear();

                if (sender.Text != "")
                {
                    if (!siteAutoCompleteTimer.IsEnabled)
                    {
                        // After a delay, start the request for auto complete
                        siteAutoCompleteTimer.Start();
                    }
                }
            }
        }

        private void AddSiteBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null)
            {
                // User hit the query button or Enter on the text box.
                // Search again for the same query
                AutoSuggestBoxTextChangedEventArgs e = new AutoSuggestBoxTextChangedEventArgs()
                {
                    Reason = AutoSuggestionBoxTextChangeReason.UserInput
                };
                AddSiteBox_TextChanged(sender, e);
            }
        }

        private async void SiteAutoCompleteTimer_Tick(object sender, object args)
        {
            AddSiteErrorText.Visibility = Visibility.Collapsed;

            try
            {
                string query = AddSiteBox.Text;
                if (query != "")
                {
                    AddSiteBox.QueryIcon = new SymbolIcon(Symbol.Sync);

                    string response = await App.Client.AutoCompleteSite(query);

                    if (AddSiteBox.Text != query)
                    {
                        // Text has changed since original query.
                        // Cancel the request and wait for the next timer tick.
                        return;
                    }

                    MainViewModel.Instance.ParseSiteAutoComplete(response);
                }
            }
            catch (Exception e)
            {
                AddSiteErrorText.Text = string.Format("Failed site query: {0}", e.Message);
                AddSiteErrorText.Visibility = Visibility.Visible;
            }

            AddSiteBox.QueryIcon = new SymbolIcon(Symbol.Find);
            siteAutoCompleteTimer.Stop();
        }

        public async void ShowAddFolderDialog()
        {
            // Reset dialog state
            AddFolderBox.Text = "";
            AddFolderFolderPicker.SelectedIndex = -1;
            AddFolderLoadingPanel.Visibility = Visibility.Collapsed;
            AddFolderErrorText.Visibility = Visibility.Collapsed;

            try
            {
                await AddFolderDialog.ShowAsync();
            }
            catch (Exception)
            {
            }
        }

        private async void AddFolderDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AddFolderErrorText.Visibility = Visibility.Collapsed;

            string folderName = AddFolderBox.Text;
            if (folderName == null || folderName == "")
            {
                args.Cancel = true;
                AddFolderErrorText.Text = "Make sure you give your folder a name.";
                AddFolderErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (!(AddFolderFolderPicker.SelectedItem is FolderLabel label))
            {
                args.Cancel = true;
                AddFolderErrorText.Text = "Make sure a parent folder is selected.";
                AddFolderErrorText.Visibility = Visibility.Visible;
                return;
            }

            string parentFolder = label.Title;
            if (parentFolder == null || parentFolder == "")
            {
                args.Cancel = true;
                AddFolderErrorText.Text = "Make sure a parent folder is selected.";
                AddFolderErrorText.Visibility = Visibility.Visible;
                return;
            }
            else if (parentFolder == "Top Level")
            {
                parentFolder = "";
            }

            var deferral = args.GetDeferral();
            AddFolderLoadingPanel.Visibility = Visibility.Visible;

            try
            {
                await MainViewModel.Instance.AddFolderAsync(folderName, parentFolder);
            }
            catch (Exception e)
            {
                args.Cancel = true;
                AddFolderLoadingPanel.Visibility = Visibility.Collapsed;
                AddFolderErrorText.Text = string.Format("Failed to add folder: {0}", e.Message);
                AddFolderErrorText.Visibility = Visibility.Visible;
            }

            AddFolderLoadingPanel.Visibility = Visibility.Collapsed;
            deferral.Complete();
        }

        public async void ShowMarkAllAsReadDialog()
        {
            // Reset dialog state
            MarkAllAsReadSlider.Value = 1;
            MarkAllAsReadLoadingPanel.Visibility = Visibility.Collapsed;
            MarkAllAsReadErrorText.Visibility = Visibility.Collapsed;

            try
            {
                await MarkAllAsReadDialog.ShowAsync();
            }
            catch (Exception)
            {
            }
        }

        private void MarkAllAsReadSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue == 0)
            {
                MarkAllAsReadMessage.Text = "Mark all stories as read";
            }
            else if (e.NewValue == 1)
            {
                MarkAllAsReadMessage.Text = "Mark all stories older than 1 day as read";
            }
            else
            {
                MarkAllAsReadMessage.Text = string.Format("Mark all stories older than {0} days as read", e.NewValue);
            }
        }

        private async void MarkAllAsReadDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            MarkAllAsReadLoadingPanel.Visibility = Visibility.Visible;

            try
            {
                await MainViewModel.Instance.MarkAllAsReadAsync((int)MarkAllAsReadSlider.Value);
            }
            catch (Exception e)
            {
                args.Cancel = true;
                MarkAllAsReadLoadingPanel.Visibility = Visibility.Collapsed;
                MarkAllAsReadErrorText.Text = string.Format("Something went wrong: {0}", e.Message);
                MarkAllAsReadErrorText.Visibility = Visibility.Visible;
            }

            MarkAllAsReadLoadingPanel.Visibility = Visibility.Collapsed;
            deferral.Complete();
        }
    }
}
