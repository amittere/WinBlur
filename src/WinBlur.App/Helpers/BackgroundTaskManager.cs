using System;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace WinBlur.App.Helpers
{
    public static class BackgroundTaskManager
    {
        private static string s_backgroundTaskNameOld = "Hypersonic_BackgroundTask";
        private static string s_backgroundTaskName = "WinBlur_BackgroundTask";
        private static string s_backgroundTaskEntryPoint = "WinBlur.BackgroundTasks.BadgeUpdateTask";

        public static async void RegisterBackgroundTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if ((task.Value.Name == s_backgroundTaskName) || (task.Value.Name == s_backgroundTaskNameOld))
                {
                    task.Value.Unregister(false);
                }
            }

            if (App.Settings.BackgroundTaskEnabled)
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                switch (status)
                {
                    case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                    case BackgroundAccessStatus.AlwaysAllowed:
                        BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                        builder.Name = s_backgroundTaskName;
                        builder.TaskEntryPoint = s_backgroundTaskEntryPoint;
                        builder.SetTrigger(new TimeTrigger((uint)App.Settings.BackgroundTaskUpdateFreq, false));
                        builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                        builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                        builder.Register();
                        break;

                    case BackgroundAccessStatus.DeniedBySystemPolicy:
                    case BackgroundAccessStatus.DeniedByUser:
                    default:
                        ClearLiveTiles();
                        break;
                }
            }
            else
            {
                ClearLiveTiles();
            }
        }

        private static void ClearLiveTiles()
        {
            TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.Clear();

            BadgeUpdater badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            badgeUpdater.Clear();
        }
    }
}
