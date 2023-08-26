using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinBlur.App.ViewModel;

namespace WinBlur.App
{
    public partial class SettingsPage : Page
    {
        private SettingsViewModel viewModel;

        public SettingsPage()
        {
            viewModel = new SettingsViewModel();
            DataContext = viewModel;

            InitializeComponent();
        }

        #region UI Event Handlers

        private void FolderFilterClearOverridesButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearFolderFilterOverrides();
            FolderFilterClearOverridesSuccessIcon.Visibility = Visibility.Visible;
        }

        private void FeedFilterClearOverridesButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearFeedFilterOverrides();
            FeedFilterClearOverridesSuccessIcon.Visibility = Visibility.Visible;
        }

        private void ReadingModeClearOverridesButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearReadingModeOverrides();
            ReadingModeClearOverridesSuccessIcon.Visibility = Visibility.Visible;
        }

        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SignOutDialog.ShowAsync();
            }
            catch (Exception)
            {
            }
        }

        private async void SignOutDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();

            await viewModel.SignOut();

            // Navigate the root page
            App.Window.RootFrame.Navigate(typeof(LoginPage), "logout");
            deferral.Complete();
        }

        #endregion UI Event Handlers
    }
}