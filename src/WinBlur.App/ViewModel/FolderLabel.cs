using Microsoft.UI.Xaml;

namespace WinBlur.App.ViewModel
{
    public class FolderLabel
    {
        public string Title { get; set; }
        public string ParentFolder { get; set; }
        public int Depth { get; set; }

        public FolderLabel(string title, string parentFolder, int depth)
        {
            Title = title;
            ParentFolder = parentFolder;
            Depth = depth;
        }

        public Thickness DepthToMargin(int offset = 0)
        {
            const int topMargin = 0;
            const int bottomMargin = 0;
            const int indent = 28;

            int leftMargin = Depth * indent;
            int rightMargin = offset;
            return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
        }
    }
}