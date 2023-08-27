using System;

namespace WinBlur.App.ViewModel
{
    public enum SortMode
    {
        All_Newest,
        All_Oldest,
        Unread_Newest,
        Unread_Oldest,
    }

    public static class SortModeHelpers
    {
        public static bool IsUnreadOnly(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.All_Newest:
                case SortMode.All_Oldest:
                    return false;

                case SortMode.Unread_Newest:
                case SortMode.Unread_Oldest:
                    return true;

                default:
                    throw new Exception("Invalid SortMode");
            }
        }

        public static bool IsNewestFirst(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.All_Newest:
                case SortMode.Unread_Newest:
                    return true;

                case SortMode.All_Oldest:
                case SortMode.Unread_Oldest:
                    return false;

                default:
                    throw new Exception("Invalid SortMode");
            }
        }
    }
}