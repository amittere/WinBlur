using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace WinBlur.App.ViewModel
{
    internal class ReadingFontViewModel
    {
        public string Label { get; set; }
        public FontFamily FontFamily { get; set; }

        public static FontWeight DefaultFontWeight = FontWeights.Normal; // Normal weight
        public FontWeight FontWeight { get; set; } = DefaultFontWeight;

        public override string ToString()
        {
            return Label;
        }

        public override bool Equals(object obj)
        {
            if (obj is ReadingFontViewModel other)
            {
                return this.Label == other.Label && this.FontFamily.Source == other.FontFamily.Source;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Label, FontFamily.Source);
        }
    }
}
