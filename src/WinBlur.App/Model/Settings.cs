using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using WinBlur.App.ViewModel;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace WinBlur.App.Model
{
    public class Settings
    {
        #region Properties

        // Theme Helpers
        private UISettings uiSettings = new UISettings();
        private Color accentColor;
        private DispatcherQueue dispatcherQueue;
        public event EventHandler ThemeChanged;

        private static uint currentVersion = 1;

        // Track whether we should show the app upgrade dialog on launch.
        public bool ShouldShowUpgradeDialog { get; set; } = false;

        // Login Settings
        public bool LoginSaved
        {
            get
            {
                PasswordCredential cred = RetrieveLoginCredentials();
                return (cred != null) || !string.IsNullOrEmpty(LoginUsername);
            }
        }

        // Username used for login, only when the account has no password.
        private string loginUsername = "";
        public string LoginUsername
        {
            get { return loginUsername; }
            set { loginUsername = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        // App Settings
        private SortMode folderSortMode = SortMode.Unread_Newest;
        public SortMode FolderSortMode
        {
            get => folderSortMode;
            set { folderSortMode = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private SortMode feedSortMode = SortMode.All_Newest;
        public SortMode FeedSortMode
        {
            get => feedSortMode;
            set { feedSortMode = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private ReadingMode defaultReadingMode = ReadingMode.Feed;
        public ReadingMode DefaultReadingMode
        {
            get => defaultReadingMode;
            set { defaultReadingMode = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private bool backgroundTaskEnabled = true;
        public bool BackgroundTaskEnabled
        {
            get => backgroundTaskEnabled;
            set { backgroundTaskEnabled = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private int backgroundTaskUpdateFreq = 30; // in minutes
        public int BackgroundTaskUpdateFreq
        {
            get => backgroundTaskUpdateFreq;
            set { backgroundTaskUpdateFreq = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private bool readAllPromptEnabled = true;
        public bool ReadAllPromptEnabled
        {
            get => readAllPromptEnabled;
            set { readAllPromptEnabled = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private bool goToNextSubscriptionAfterMarkingAsRead = false;
        public bool GoToNextSubscriptionAfterMarkingAsRead
        {
            get => goToNextSubscriptionAfterMarkingAsRead;
            set { goToNextSubscriptionAfterMarkingAsRead = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private MarkAsReadMode markAsRead = MarkAsReadMode.WhenSelectionChanges;
        public MarkAsReadMode MarkAsRead
        {
            get => markAsRead;
            set { markAsRead = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private int markAsReadDelay = 0; // in seconds
        public int MarkAsReadDelay
        {
            get => markAsReadDelay;
            set { markAsReadDelay = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private AppThemeMode appTheme2 = AppThemeMode.UseWindowsTheme;
        public AppThemeMode AppTheme2
        {
            get => appTheme2;
            set { appTheme2 = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private ReadingThemeMode readingTheme = ReadingThemeMode.UseWindowsTheme;
        public ReadingThemeMode ReadingTheme
        {
            get => readingTheme;
            set { readingTheme = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private FeedMode feedFilterMode = FeedMode.All;
        public FeedMode FeedFilterMode
        {
            get => feedFilterMode;
            set { feedFilterMode = value; SaveSetting(ApplicationData.Current.LocalSettings, (int)value); }
        }

        private bool openLinksInBrowser = false;
        public bool OpenLinksInBrowser
        {
            get => openLinksInBrowser;
            set { openLinksInBrowser = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        private bool showImagePreviews = true;
        public bool ShowImagePreviews
        {
            get => showImagePreviews;
            set { showImagePreviews = value; SaveSetting(ApplicationData.Current.LocalSettings, value); }
        }

        public Dictionary<string, SortMode> SortModeSettings { get; private set; } = new Dictionary<string, SortMode>();

        public Dictionary<string, ReadingMode> ReadingModeSettings { get; private set; } = new Dictionary<string, ReadingMode>();

        public Dictionary<string, bool> FolderCompressSettings { get; private set; } = new Dictionary<string, bool>();

        #endregion Properties

        #region Initialization

        public Settings()
        {
            accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;
        }

        #endregion Initialization

        #region Settings Save/Load

        public async Task Initialize()
        {
            // Check to see if this user's settings have been migrated to LocalSettings.
            var localSettings = ApplicationData.Current.LocalSettings;
            uint version = ApplicationData.Current.Version;
            if (version == currentVersion)
            {
                // Already migrated - just load from local settings.
                LoadSettings(localSettings);
            }
            else
            {
                // Upgrade settings version
                await ApplicationData.Current.SetVersionAsync(currentVersion, OnApplicationDataSetVersion);
            }
        }

        private void OnApplicationDataSetVersion(SetVersionRequest request)
        {
            if (request.CurrentVersion == 0)
            {
                // Need to migrate any settings from RoamingSettings => LocalSettings
                LoadSettings(ApplicationData.Current.RoamingSettings);
                SaveSettings(ApplicationData.Current.LocalSettings);

                if (LoginSaved)
                {
                    // Show upgrade dialog on first launch if the user already has a login saved
                    // (i.e. is an existing user) and is upgrading to the latest version.
                    ShouldShowUpgradeDialog = true;
                }
            }
        }

        private T LoadSetting<T>(ApplicationDataContainer container, string key, T defaultValue)
        {
            if (container.Values.ContainsKey(key))
            {
                return (T)container.Values[key];
            }
            else
            {
                return defaultValue;
            }
        }

        private void LoadSettings(ApplicationDataContainer container)
        {
            loginUsername = LoadSetting(container, nameof(LoginUsername), LoginUsername);
            folderSortMode = LoadSetting(container, nameof(FolderSortMode), FolderSortMode);
            feedSortMode = LoadSetting(container, nameof(FeedSortMode), FeedSortMode);
            defaultReadingMode = LoadSetting(container, nameof(DefaultReadingMode), DefaultReadingMode);
            backgroundTaskEnabled = LoadSetting(container, nameof(BackgroundTaskEnabled), BackgroundTaskEnabled);
            backgroundTaskUpdateFreq = LoadSetting(container, nameof(BackgroundTaskUpdateFreq), BackgroundTaskUpdateFreq);
            readAllPromptEnabled = LoadSetting(container, nameof(ReadAllPromptEnabled), ReadAllPromptEnabled);
            goToNextSubscriptionAfterMarkingAsRead = LoadSetting(container, nameof(GoToNextSubscriptionAfterMarkingAsRead), GoToNextSubscriptionAfterMarkingAsRead);
            markAsRead = LoadSetting(container, nameof(MarkAsRead), MarkAsRead);
            markAsReadDelay = LoadSetting(container, nameof(MarkAsReadDelay), MarkAsReadDelay);
            feedFilterMode = LoadSetting(container, nameof(FeedFilterMode), FeedFilterMode);
            openLinksInBrowser = LoadSetting(container, nameof(OpenLinksInBrowser), OpenLinksInBrowser);
            showImagePreviews = LoadSetting(container, nameof(ShowImagePreviews), ShowImagePreviews);
            appTheme2 = LoadSetting(container, nameof(AppTheme2), AppTheme2);
            readingTheme = LoadSetting(container, nameof(ReadingTheme), ReadingTheme);

            var sortModeSettings = LoadSetting<ApplicationDataCompositeValue>(container, nameof(SortModeSettings), null);
            if (sortModeSettings != null)
            {
                SortModeSettings.Clear();
                foreach (var item in sortModeSettings)
                {
                    SortModeSettings[item.Key] = (SortMode)item.Value;
                }
            }

            var readingModeSettings = LoadSetting<ApplicationDataCompositeValue>(container, nameof(ReadingModeSettings), null);
            if (readingModeSettings != null)
            {
                ReadingModeSettings.Clear();
                foreach (var item in readingModeSettings)
                {
                    ReadingModeSettings[item.Key] = (ReadingMode)item.Value;
                }
            }

            var folderCompressSettings = LoadSetting<ApplicationDataCompositeValue>(container, nameof(FolderCompressSettings), null);
            if (folderCompressSettings != null)
            {
                FolderCompressSettings.Clear();
                foreach (var item in folderCompressSettings)
                {
                    FolderCompressSettings[item.Key] = (bool)item.Value;
                }
            }
        }

        private void SaveSetting<T>(ApplicationDataContainer container, T value, [CallerMemberName] string key = null)
        {
            container.Values[key] = value;
        }

        public void SaveSettings(ApplicationDataContainer container)
        {
            SaveSetting(container, LoginUsername, nameof(LoginUsername));
            SaveSetting(container, (int)FolderSortMode, nameof(FolderSortMode));
            SaveSetting(container, (int)FeedSortMode, nameof(FeedSortMode));
            SaveSetting(container, (int)DefaultReadingMode, nameof(DefaultReadingMode));
            SaveSetting(container, backgroundTaskEnabled, nameof(BackgroundTaskEnabled));
            SaveSetting(container, BackgroundTaskUpdateFreq, nameof(BackgroundTaskUpdateFreq));
            SaveSetting(container, readAllPromptEnabled, nameof(ReadAllPromptEnabled));
            SaveSetting(container, GoToNextSubscriptionAfterMarkingAsRead, nameof(GoToNextSubscriptionAfterMarkingAsRead));
            SaveSetting(container, (int)MarkAsRead, nameof(MarkAsRead));
            SaveSetting(container, MarkAsReadDelay, nameof(MarkAsReadDelay));
            SaveSetting(container, (int)FeedFilterMode, nameof(FeedFilterMode));
            SaveSetting(container, OpenLinksInBrowser, nameof(OpenLinksInBrowser));
            SaveSetting(container, ShowImagePreviews, nameof(ShowImagePreviews));
            SaveSetting(container, (int)AppTheme2, nameof(AppTheme2));
            SaveSetting(container, (int)ReadingTheme, nameof(ReadingTheme));

            // Sort mode settings
            if (SortModeSettings.Count > 0)
            {
                ApplicationDataCompositeValue sortModeSettings = new ApplicationDataCompositeValue();
                foreach (var item in SortModeSettings)
                {
                    sortModeSettings[item.Key] = (int)item.Value;
                }
                SaveSetting(container, sortModeSettings, nameof(SortModeSettings));
            }

            // Reading mode settings
            if (ReadingModeSettings.Count > 0)
            {
                ApplicationDataCompositeValue readingModeSettings = new ApplicationDataCompositeValue();
                foreach (var item in ReadingModeSettings)
                {
                    readingModeSettings[item.Key] = (int)item.Value;
                }
                SaveSetting(container, readingModeSettings, nameof(ReadingModeSettings));
            }

            // Folder compress settings
            if (FolderCompressSettings.Count > 0)
            {
                ApplicationDataCompositeValue folderCompressSettings = new ApplicationDataCompositeValue();
                foreach (var item in FolderCompressSettings)
                {
                    folderCompressSettings[item.Key] = item.Value;
                }
                SaveSetting(container, folderCompressSettings, nameof(FolderCompressSettings));
            }
        }

        public void SetSortMode(string key, SortMode value)
        {
            SortModeSettings[key] = value;

            var localSettings = ApplicationData.Current.LocalSettings;

            var sortModeSettingsValue = LoadSetting<ApplicationDataCompositeValue>(localSettings, nameof(SortModeSettings), null);
            if (sortModeSettingsValue == null)
            {
                sortModeSettingsValue = new ApplicationDataCompositeValue();
            }

            sortModeSettingsValue[key] = (int)value;
            SaveSetting(localSettings, sortModeSettingsValue, nameof(SortModeSettings));
        }

        public void SetReadingMode(string key, ReadingMode value)
        {
            ReadingModeSettings[key] = value;

            var localSettings = ApplicationData.Current.LocalSettings;

            var readingModeSettingsValue = LoadSetting<ApplicationDataCompositeValue>(localSettings, nameof(ReadingModeSettings), null);
            if (readingModeSettingsValue == null)
            {
                readingModeSettingsValue = new ApplicationDataCompositeValue();
            }

            readingModeSettingsValue[key] = (int)value;
            SaveSetting(localSettings, readingModeSettingsValue, nameof(ReadingModeSettings));
        }

        public void SetFolderCompress(string key, bool value)
        {
            FolderCompressSettings[key] = value;

            var localSettings = ApplicationData.Current.LocalSettings;

            var folderCompressSettingsValue = LoadSetting<ApplicationDataCompositeValue>(localSettings, nameof(FolderCompressSettings), null);
            if (folderCompressSettingsValue == null)
            {
                folderCompressSettingsValue = new ApplicationDataCompositeValue();
            }

            folderCompressSettingsValue[key] = value;
            SaveSetting(localSettings, folderCompressSettingsValue, nameof(FolderCompressSettings));
        }

        public void DeleteSettings()
        {
            ApplicationData.Current.LocalSettings.Values.Clear();
            ApplicationData.Current.RoamingSettings.Values.Clear();
        }

        #endregion

        #region Login Credentials

        public bool SaveLoginCredentials(string username, string password)
        {
            if (!string.IsNullOrEmpty(password))
            {
                // First clear all credentials from the passwordvault
                ClearLoginCredentials();

                try
                {
                    // Then add the new credential
                    var vault = new PasswordVault();
                    vault.Add(new PasswordCredential("WinBlur", username, password));
                }
                catch (Exception)
                {
                    // TODO: log some telemetry here
                    return false;
                }
            }
            else
            {
                // Passwordless login. Because you can't save an empty credential into the CredentialVault,
                // we just have to save the username to roaming settings.
                LoginUsername = username;
            }

            return true;
        }

        public bool ClearLoginCredentials()
        {
            try
            {
                var vault = new PasswordVault();
                IReadOnlyList<PasswordCredential> credentialList = vault.RetrieveAll();
                foreach (PasswordCredential cred in credentialList)
                {
                    vault.Remove(cred);
                }
            }
            catch (Exception)
            {
                // TODO: log some telemetry here
                return false;
            }
            finally
            {
                LoginUsername = "";
            }
            return true;
        }

        public PasswordCredential RetrieveLoginCredentials()
        {
            try
            {
                var vault = new PasswordVault();
                IReadOnlyList<PasswordCredential> credentialList = vault.RetrieveAll();
                if (credentialList != null && credentialList.Count > 0)
                {
                    return credentialList[0];
                }
            }
            catch (Exception)
            {
                // TODO: log some telemetry here
            }

            return null;
        }

        #endregion

        #region Theme

        private async void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            Color newAccentColor = sender.GetColorValue(UIColorType.Accent);
            await dispatcherQueue.EnqueueAsync(() =>
            {
                UpdateAppTheme();
                if (accentColor != newAccentColor)
                {
                    accentColor = newAccentColor;
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public ElementTheme GetElementThemeFromAppTheme(AppThemeMode themeMode)
        {
            if (themeMode == AppThemeMode.UseWindowsTheme)
            {
                return AppThemeToElementTheme(Application.Current.RequestedTheme);
            }
            else
            {
                return AppThemeToElementTheme((ApplicationTheme)themeMode);
            }
        }

        public void UpdateAppTheme()
        {
            if (App.Window.Content is FrameworkElement element)
            {
                var oldTheme = element.RequestedTheme;
                element.RequestedTheme = GetElementThemeFromAppTheme(AppTheme2);
                if (element.RequestedTheme != oldTheme)
                {
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Force toggles the app's theme between Light and Dark. Only used in TestMode.
        /// </summary>
        public void ToggleTheme()
        {
            if (App.Window.Content is FrameworkElement element)
            {
                switch (element.RequestedTheme)
                {
                    case ElementTheme.Default:
                    case ElementTheme.Light:
                        element.RequestedTheme = ElementTheme.Dark;
                        break;

                    case ElementTheme.Dark:
                        element.RequestedTheme = ElementTheme.Light;
                        break;
                }

                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ElementTheme AppThemeToElementTheme(ApplicationTheme appTheme)
        {
            switch (appTheme)
            {
                case ApplicationTheme.Light: return ElementTheme.Light;
                case ApplicationTheme.Dark: return ElementTheme.Dark;
                default: return ElementTheme.Default;
            }
        }

        #endregion
    }

    public enum MarkAsReadMode
    {
        Never,
        WhenSelectionChanges,
        AfterDelay
    }

    public enum AppThemeMode
    {
        Light,
        Dark,
        UseWindowsTheme
    }

    public enum ReadingThemeMode
    {
        UseWindowsTheme,
        Light,
        Sepia,
        Dark,
        Black
    }
}
