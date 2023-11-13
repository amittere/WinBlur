using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using WinBlur.App.Helpers;
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

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            PersistenceId = "WinBlur.App.MainWindow";

            Activated += MainWindow_Activated;
            Title = AppTitleText.Text;
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
    }
}
