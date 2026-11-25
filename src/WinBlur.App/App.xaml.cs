using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.IO;
using WinBlur.App.Helpers;
using WinBlur.App.Model;
using WinBlur.Shared;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;

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
        private AppInstance appInstance;
        private DispatcherQueue dispatcherQueue;

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
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            appInstance = AppInstance.GetCurrent();
            appInstance.Activated += OnActivated;

            var mainInstance = AppInstance.FindOrRegisterForKey("main");
            if (!mainInstance.IsCurrent)
            {
                await mainInstance.RedirectActivationToAsync(appInstance.GetActivatedEventArgs());
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

            if (Settings.LoginSaved || Client.HasValidSessionCookie())
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

        private void OnActivated(object sender, AppActivationArguments args)
        {
            if (args.Kind == ExtendedActivationKind.Protocol)
            {
                var protocolArgs = (ProtocolActivatedEventArgs)args.Data;
                dispatcherQueue.TryEnqueue(() =>
                {
                    if (protocolArgs.Uri.Authority == NewsBlurOAuth.CallbackAuthority)
                    {
                        if (OAuth2Manager.CompleteAuthRequest(protocolArgs.Uri))
                        {
                            Window.Activate();
                        }
                    }
                });
            }
        }

        private void SetWindowIcon()
        {
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(WindowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, "Assets\\AppLogo.ico"));
        }
    }
}
