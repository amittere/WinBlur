using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace WinBlur.App.Helpers
{
    public static class Converters
    {
        public enum IconGlyph
        {
            Unknown,
            CommentsEmpty,
            CommentsFull,
            ArrowUp,
            ArrowDown,
            ArrowRight,
            Folder,
            Globe,
            Web,
            People,
            Sites,
            OutlineStar,
            Tag,
        }

        public static string IconGlyphToString(IconGlyph glyph)
        {
            switch (glyph)
            {
                case IconGlyph.CommentsEmpty: return "\uE15F";
                case IconGlyph.CommentsFull: return "\uE25C";
                case IconGlyph.ArrowUp: return "\uE010";
                case IconGlyph.ArrowDown: return "\uE011";
                case IconGlyph.ArrowRight: return "\uE00F";
                case IconGlyph.Folder: return "\uE8B7";
                case IconGlyph.Globe: return "\uE128";
                case IconGlyph.Web: return "\uE774";
                case IconGlyph.People: return "\uE125";
                case IconGlyph.Sites: return "\uE12A";
                case IconGlyph.OutlineStar: return "\uE1CE";
                case IconGlyph.Tag: return "\uE1CB";
                default: return "";
            }
        }
    }

    public class DepthToMarginConverter : IValueConverter
    {
        private int topMargin = 0;
        private int bottomMargin = 0;
        private int indent = 28;

        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            int marginOffset = parameter != null ? int.Parse((string)parameter) : 0;
            int leftMargin = (int)value * indent;
            int rightMargin = marginOffset;
            return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}
