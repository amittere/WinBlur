using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using WinBlur.App.Helpers;
using WinBlur.Shared;
using Windows.System;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

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
            set { _isSigningIn = value; NotifyPropertyChanged("IsSigningIn"); }
        }

        private string _signInProgressText;
        public string SignInProgressText
        {
            get { return _signInProgressText; }
            set { _signInProgressText = value; NotifyPropertyChanged("SignInProgressText"); }
        }

        #endregion Properties

        public LoginPage()
        {
            this.InitializeComponent();

            DataContext = this;
            IsSigningIn = false;
            SignInProgressText = "";

        }

        #region Navigation

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            if (!IsSigningIn)
            {
                if (loginPanel.Visibility == Visibility.Visible)
                {
                    usernameBox.Text = "";
                    passwordBox.Password = "";

                    splashPanel.Visibility = Visibility.Visible;
                    loginPanel.Visibility = Visibility.Collapsed;

                    return true;
                }
                else if (createAccountPanel.Visibility == Visibility.Visible)
                {
                    createUsernameBox.Text = "";
                    createEmailBox.Text = "";
                    createPasswordBox.Password = "";

                    splashPanel.Visibility = Visibility.Visible;
                    createAccountPanel.Visibility = Visibility.Collapsed;

                    return true;
                }
            }
            return false;
        }

        private void LoginSplashPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = On_BackRequested();
        }

        #endregion Navigation

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            splashPanel.Visibility = Visibility.Collapsed;
            createAccountPanel.Visibility = Visibility.Visible;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            splashPanel.Visibility = Visibility.Collapsed;
            loginPanel.Visibility = Visibility.Visible;
        }

        private async void SubmitLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Get username and password from textfields
            string username = usernameBox.Text;
            string password = passwordBox.Password;

            // Basic error checking
            if (username == "")
            {
                // Display error message
                DialogHelper.DisplayErrorMessage("Login failed", "Username is required.");
                return;
            }

            // Disable login button and change its text
            IsSigningIn = true;
            SignInProgressText = "Signing in...";

            try
            {
                string s = await App.Client.Login(username, password);
                JObject response = JObject.Parse(s);
                if ((int)response["code"] == 1 && (bool)response["authenticated"] == true)
                {
                    bool success = App.Settings.SaveLoginCredentials(username, password);
                    if (!success)
                    {
                        DialogHelper.DisplayErrorMessage("Failed to save login credentials",
                            "Please try again.");
                    }
                    else
                    {
                        Frame.Navigate(typeof(MainPage), "login");
                    }
                }
                else
                {
                    // Look for error messages in the API response
                    List<string> errorList = new List<string>();
                    if (response["errors"] is JObject joErrors)
                    {
                        foreach (JToken t in joErrors.Children())
                        {
                            if (((JProperty)t).Value is JArray errorArray)
                            {
                                foreach (JToken errorString in errorArray)
                                {
                                    errorList.Add(ParseHelper.ParseValueRef(errorString, ""));
                                }
                            }
                        }
                    }

                    // Display error message
                    string description = string.Join("\n", errorList);
                    DialogHelper.DisplayErrorMessage("Account creation failed", description);
                }
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }

            IsSigningIn = false;
            SignInProgressText = "";
        }

        private async void SubmitCreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            // Get credentials from textfields
            string username = createUsernameBox.Text;
            string password = createPasswordBox.Password;
            string email = createEmailBox.Text;

            // Basic error checking
            if (username == "" || email == "")
            {
                // Display error message
                DialogHelper.DisplayErrorMessage("Account creation failed", "Both username and email address are required.");
                return;
            }

            // Disable login button and change its text
            IsSigningIn = true;
            SignInProgressText = "Creating account...";

            try
            {
                string s = await App.Client.Signup(username, password, email);
                JObject response = JObject.Parse(s);
                if ((int)response["code"] == 1 && (bool)response["authenticated"] == true)
                {
                    bool success = App.Settings.SaveLoginCredentials(username, password);
                    if (!success)
                    {
                        DialogHelper.DisplayErrorMessage("Failed to save login credentials",
                            "Please try again.");
                    }
                    else
                    {
                        Frame.Navigate(typeof(MainPage), "login");
                    }
                }
                else
                {
                    // Look for error messages in the API response
                    List<string> errorList = new List<string>();
                    if (response["errors"] is JObject joErrors)
                    {
                        foreach (JToken t in joErrors.Children())
                        {
                            if (((JProperty)t).Value is JArray errorArray)
                            {
                                foreach (JToken errorString in errorArray)
                                {
                                    errorList.Add(ParseHelper.ParseValueRef(errorString, ""));
                                }
                            }
                        }
                    }

                    // Display error message
                    string description = string.Join("\n", errorList);
                    DialogHelper.DisplayErrorMessage("Account creation failed", description);
                }
            }
            catch (Exception)
            {
                DialogHelper.DisplayNetworkError();
            }

            IsSigningIn = false;
            SignInProgressText = "";
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this,
    new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        private void usernameBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Set focus to the password box
                passwordBox.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void passwordBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Submit login attempt
                submitLoginButton.Focus(FocusState.Programmatic);
                SubmitLoginButton_Click(sender, null);
                e.Handled = true;
            }
        }

        private void createUsernameBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Set focus to the email box
                createEmailBox.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void createEmailBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Set focus to the password box
                createPasswordBox.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void createPasswordBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Set focus to the password box
                submitCreateAccountButton.Focus(FocusState.Programmatic);
                SubmitCreateAccountButton_Click(sender, null);
                e.Handled = true;
            }
        }
    }
}
