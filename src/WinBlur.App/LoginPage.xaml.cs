using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Threading;
using WinBlur.App.Helpers;
using WinBlur.Shared;

namespace WinBlur.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page, INotifyPropertyChanged
    {
        #region Properties

        private bool _isSigningIn;
        public bool IsSigningIn
        {
            get { return _isSigningIn; }
            set { _isSigningIn = value; NotifyPropertyChanged(nameof(IsSigningIn)); }
        }

        private string _signInProgressText;
        public string SignInProgressText
        {
            get { return _signInProgressText; }
            set { _signInProgressText = value; NotifyPropertyChanged(nameof(SignInProgressText)); }
        }

        private CancellationTokenSource signInCancellationTokenSource;

        #endregion Properties

        public LoginPage()
        {
            this.InitializeComponent();

            DataContext = this;
            IsSigningIn = false;
            SignInProgressText = "";
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            IsSigningIn = true;
            SignInProgressText = "Complete sign-in in your browser...";
            signInCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            try
            {
                await NewsBlurOAuth.SignInAsync(App.Window.AppWindow.Id, signInCancellationTokenSource.Token);
                App.Settings.ClearLoginCredentials();
                App.Client.DeleteCookies();
                Frame.Navigate(typeof(MainPage), "login");
            }
            catch (OperationCanceledException)
            {
                // Silently handle cancellation, UI will go back to main login screen
            }
            catch (Exception ex)
            {
                DialogHelper.DisplayErrorMessage("Sign-in failed", ex.Message);
            }

            signInCancellationTokenSource = null;
            IsSigningIn = false;
            SignInProgressText = "";
        }

        private void LoginCancelButton_Click(object sender, RoutedEventArgs e)
        {
            signInCancellationTokenSource?.Cancel();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
