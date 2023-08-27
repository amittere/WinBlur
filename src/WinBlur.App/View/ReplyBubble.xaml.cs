using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinBlur.App.Model;

namespace WinBlur.App.View
{
    public partial class ReplyBubble : UserControl
    {
        public Button EditButton { get { return editButton; } }

        public ReplyBubble()
        {
            InitializeComponent();

            editButton.Tag = this;
        }

        private void ReplyBubble_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is Comment comment)) return;

            // If this is the logged in user's comment, show edit button instead of like button
            if (comment.User.ID == App.Client.MyUserID)
            {
                editButton.Visibility = Visibility.Visible;
            }
        }
    }
}
