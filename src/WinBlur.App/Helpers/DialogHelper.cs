using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace WinBlur.App.Helpers
{
    public static class DialogHelper
    {
        public static async void DisplayErrorMessage(string title, string description)
        {
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = title,
                    Content = description,
                    PrimaryButtonText = "Ok",
                    XamlRoot = App.Window.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception)
            {
                // Suppress extra dialogs
            }
        }

        public static async void DisplayConfirmationMessage(
            string title,
            string description,
            object dialogTag,
            TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> confirmHandler)
        {
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = title,
                    Content = description,
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Yes",
                    IsSecondaryButtonEnabled = true,
                    SecondaryButtonText = "No",
                    Tag = dialogTag,
                    XamlRoot = App.Window.Content.XamlRoot
                };
                dialog.PrimaryButtonClick += confirmHandler;
                await dialog.ShowAsync();
            }
            catch (Exception)
            {

            }
        }

        public static void DisplayInfoMessage(string title, string description)
        {
            DisplayErrorMessage(title, description);
        }

        public static void DisplayNetworkError()
        {
            DisplayErrorMessage("No internet connection", "Please check your network settings and try again.");
        }

        public static void DisplaySyncError()
        {
            DisplayErrorMessage("Synchronization failed", "Please check your internet connection.");
        }

        public static void DisplayComingSoonMessage()
        {
            DisplayErrorMessage("Coming soon!", "Please check back later.");
        }
    }
}