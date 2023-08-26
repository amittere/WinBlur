using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using WinBlur.App.Helpers;
using WinBlur.App.Model;
using Windows.ApplicationModel;

namespace WinBlur.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow Window { get; private set; }
        public static IntPtr WindowHandle { get; private set; }
        public static NewsBlurClient Client { get; set; }
        public static Settings Settings { get; set; }
        public static TestModeHelper TestModeHelper { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // TODO This code defaults the app to a single instance app. If you need multi instance app, remove this part.
            // Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle#single-instancing-in-applicationonlaunched
            // If this is the first instance launched, then register it as the "main" instance.
            // If this isn't the first instance launched, then "main" will already be registered,
            // so retrieve it.
            var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("main");
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            // If the instance that's executing the OnLaunched handler right now
            // isn't the "main" instance.
            if (!mainInstance.IsCurrent)
            {
                // Redirect the activation (and args) to the "main" instance, and exit.
                await mainInstance.RedirectActivationToAsync(activatedEventArgs);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            // Initialize global objects
            Window = new MainWindow();

            Settings = new Settings();
            await Settings.Initialize();
            Settings.UpdateAppTheme();

            Client = new NewsBlurClient();

            TestModeHelper = new TestModeHelper();
            TestModeHelper.AddAccelerator(Window.RootFrame);

            if (Settings.LoginSaved)
            {
                Window.RootFrame.Navigate(typeof(MainPage), args.Arguments);
            }
            else
            {
                Window.RootFrame.Navigate(typeof(LoginPage), args.Arguments);
            }

            Window.Activate();

            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);
            SetWindowIcon();
        }

        private void SetWindowIcon()
        {
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(WindowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, "Assets\\AppLogo.ico"));
        }
    }
}
