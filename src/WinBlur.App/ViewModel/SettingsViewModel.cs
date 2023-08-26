using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WinBlur.App.Helpers;
using WinBlur.App.Model;
using WinBlur.Shared;
using Windows.ApplicationModel;

namespace WinBlur.App.ViewModel
{
    internal class SettingsViewModel : INotifyPropertyChanged
    {
        public int FolderSortMode
        {
            get { return (int)App.Settings.FolderSortMode; }
            set { App.Settings.FolderSortMode = (SortMode)value; NotifyPropertyChanged("FolderSortMode"); }
        }
        public int FeedSortMode
        {
            get { return (int)App.Settings.FeedSortMode; }
            set { App.Settings.FeedSortMode = (SortMode)value; NotifyPropertyChanged("FeedSortMode"); }
        }
        public int DefaultReadingMode
        {
            get { return (int)App.Settings.DefaultReadingMode; }
            set { App.Settings.DefaultReadingMode = (ReadingMode)value; NotifyPropertyChanged("DefaultReadingMode"); }
        }
        public bool BackgroundTaskEnabled
        {
            get { return App.Settings.BackgroundTaskEnabled; }
            set
            {
                App.Settings.BackgroundTaskEnabled = value;
                BackgroundTaskManager.RegisterBackgroundTask();
                NotifyPropertyChanged("BackgroundTaskEnabled");
            }
        }
        public int BackgroundTaskUpdateFreq
        {
            get { return BackgroundTaskFreqToPickerIndex(App.Settings.BackgroundTaskUpdateFreq); }
            set
            {
                App.Settings.BackgroundTaskUpdateFreq = PickerIndexToBackgroundTaskFreq(value);
                BackgroundTaskManager.RegisterBackgroundTask();
                NotifyPropertyChanged("BackgroundTaskUpdateFreq");
            }
        }
        public bool ReadAllPromptEnabled
        {
            get { return App.Settings.ReadAllPromptEnabled; }
            set { App.Settings.ReadAllPromptEnabled = value; NotifyPropertyChanged("ReadAllPromptEnabled"); }
        }
        public bool GoToNextSubscriptionAfterMarkingAsRead
        {
            get { return App.Settings.GoToNextSubscriptionAfterMarkingAsRead; }
            set { App.Settings.GoToNextSubscriptionAfterMarkingAsRead = value; NotifyPropertyChanged("GoToNextSubscriptionAfterMarkingAsRead"); }
        }
        public MarkAsReadMode MarkAsRead
        {
            get { return App.Settings.MarkAsRead; }
            set
            {
                App.Settings.MarkAsRead = value;
                NotifyPropertyChanged("MarkAsRead");
                NotifyPropertyChanged("MarkAsReadNever");
                NotifyPropertyChanged("MarkAsReadWhenSelectionChanges");
                NotifyPropertyChanged("MarkAsReadAfterDelay");
            }
        }
        public bool MarkAsReadNever
        {
            get { return MarkAsRead == MarkAsReadMode.Never; }
            set { if (value) MarkAsRead = MarkAsReadMode.Never; }
        }
        public bool MarkAsReadWhenSelectionChanges
        {
            get { return MarkAsRead == MarkAsReadMode.WhenSelectionChanges; }
            set { if (value) MarkAsRead = MarkAsReadMode.WhenSelectionChanges; }
        }
        public bool MarkAsReadAfterDelay
        {
            get { return MarkAsRead == MarkAsReadMode.AfterDelay; }
            set { if (value) MarkAsRead = MarkAsReadMode.AfterDelay; }
        }
        public string MarkAsReadDelay
        {
            get { return App.Settings.MarkAsReadDelay.ToString(); }
            set
            {
                if (int.TryParse(value, out int intValue))
                {
                    App.Settings.MarkAsReadDelay = intValue;
                    NotifyPropertyChanged("MarkAsReadDelay");
                }
            }
        }
        public int AppTheme
        {
            get { return (int)App.Settings.AppTheme2; }
            set
            {
                App.Settings.AppTheme2 = (AppThemeMode)value;
                App.Settings.UpdateAppTheme();
                NotifyPropertyChanged("AppTheme");
            }
        }
        public bool OpenLinksInBrowser
        {
            get { return App.Settings.OpenLinksInBrowser; }
            set { App.Settings.OpenLinksInBrowser = value; NotifyPropertyChanged("OpenLinksInBrowser"); }
        }
        public bool ShowImagePreviews
        {
            get { return App.Settings.ShowImagePreviews; }
            set { App.Settings.ShowImagePreviews = value; NotifyPropertyChanged("ShowImagePreviews"); }
        }

        public bool ShowFeedFilterClearOverrides
        {
            get
            {
                // Look through SortModeSettings for a feed override
                foreach (var item in App.Settings.SortModeSettings)
                {
                    if (int.TryParse(item.Key, out int id))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool ShowFolderFilterClearOverrides
        {
            get
            {
                // Look through SortModeSettings for a folder override
                foreach (var item in App.Settings.SortModeSettings)
                {
                    if (!int.TryParse(item.Key, out _))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool ShowReadingModeClearOverrides
        {
            get { return App.Settings.ReadingModeSettings.Count > 0; }
        }
        public string Username
        {
            get
            {
                if (!string.IsNullOrEmpty(App.Settings.LoginUsername))
                {
                    return App.Settings.LoginUsername;
                }
                else
                {
                    // Try to retrieve username from login credentials
                    var credential = App.Settings.RetrieveLoginCredentials();
                    if (credential != null)
                    {
                        return credential.UserName;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
        }
        public string PackageName { get { return Package.Current.DisplayName; } }
        public string VersionNumber
        {
            get
            {
                PackageVersion version = Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        public SettingsViewModel() { }

        public int ClearFeedFilterOverrides()
        {
            int count = 0;
            foreach (var item in App.Settings.SortModeSettings)
            {
                int id = -1;
                bool success = int.TryParse(item.Key, out id);
                if (success)
                {
                    // This is a feed. remove it and increment count
                    App.Settings.SortModeSettings.Remove(item.Key);
                    count++;
                }
            }
            NotifyPropertyChanged("ShowFeedFilterClearOverrides");

            return count;
        }

        public int ClearFolderFilterOverrides()
        {
            int count = 0;
            foreach (var item in App.Settings.SortModeSettings)
            {
                int id = -1;
                bool success = int.TryParse(item.Key, out id);
                if (!success)
                {
                    // This is a folder. remove it and increment count
                    App.Settings.SortModeSettings.Remove(item.Key);
                    count++;
                }
            }
            NotifyPropertyChanged("ShowFolderFilterClearOverrides");

            return count;
        }

        public int ClearReadingModeOverrides()
        {
            int count = App.Settings.ReadingModeSettings.Count;
            App.Settings.ReadingModeSettings.Clear();
            NotifyPropertyChanged("ShowReadingModeClearOverrides");
            return count;
        }

        public async Task SignOut()
        {
            try
            {
                await App.Client.Logout();
                // We don't really care if this succeeds or not, because it just
                // invalidates the cookie.
            }
            catch (Exception)
            {
            }

            // Clear out settings
            App.Settings.DeleteSettings();
            App.Settings.ClearLoginCredentials();

            // Clear app badge
            BadgeHelper.UpdateNumericBadge(0);

            // Delete the cookie from the httpclient
            App.Client.DeleteCookies();

            // Reset application global objects
            App.Client = new NewsBlurClient();
            App.Settings = new Settings();
        }

        private int BackgroundTaskFreqToPickerIndex(int freq)
        {
            if (freq == 15) return 0;
            if (freq == 30) return 1;
            if (freq == 60) return 2;
            if (freq == 120) return 3;
            if (freq == 240) return 4;
            else return 1;
        }

        private int PickerIndexToBackgroundTaskFreq(int index)
        {
            if (index == 0) return 15;
            if (index == 1) return 30;
            if (index == 2) return 60;
            if (index == 3) return 120;
            if (index == 4) return 240;
            else return 30;
        }
    }
}
