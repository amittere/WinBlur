using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinBlur.App.ViewModel
{
    public enum ReadingMode
    {
        Feed,
        Text
    }

    public static class ReadingModeHelpers
    {
        public static string GetTooltipString(ReadingMode readingMode)
        {
            switch (readingMode)
            {
                case ReadingMode.Feed:
                    return "Reading mode: Feed";
                case ReadingMode.Text:
                    return "Reading mode: Text";
                default:
                    throw new ArgumentException("Invalid ReadingMode");
            }
        }
    }
}
