using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinBlur.App.Model;
using Windows.UI;

namespace WinBlur.App.ViewModel
{
    internal class ReadingThemeViewModel
    {
        public string Label { get; set; }
        public ReadingThemeMode ThemeMode { get; set; }

        public override string ToString()
        {
            return Label;
        }
    }
}
