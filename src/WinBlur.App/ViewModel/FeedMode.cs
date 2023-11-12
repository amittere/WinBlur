using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinBlur.App.ViewModel
{
    public enum FeedMode
    {
        All,
        Unread,
        Focus,
        Saved
    }

    public static class FeedModeHelpers
    {
        public static string GetTooltipString(FeedMode mode)
        {
            switch (mode)
            {
                case FeedMode.All:
                    return "Filter: All";
                case FeedMode.Unread:
                    return "Filter: Unread";
                case FeedMode.Focus:
                    return "Filter: Focus";
                case FeedMode.Saved:
                    return "Filter: Saved";
                default:
                    throw new ArgumentException("Invalid FeedMode");
            }
        }
    }
}
