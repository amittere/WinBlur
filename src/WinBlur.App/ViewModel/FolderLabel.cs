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
    }
}