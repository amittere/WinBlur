using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinBlur.App.Model;

namespace WinBlur.App.View
{
    public partial class CommentBubble : UserControl
    {
        public Button LikeButton { get { return likeButton; } }
        public Button EditButton { get { return editButton; } }
        public Button ReplyButton { get { return replyButton; } }

        public CommentBubble()
        {
            InitializeComponent();

            // Tag the reply button with its owner so that we can handle replies properly
            editButton.Tag = this;
            replyButton.Tag = this;
        }

        private void CommentBubble_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is Comment comment)) return;

            // If the comment has no text, adjust UI to match
            if (string.IsNullOrEmpty(comment.CommentString))
            {
                commentText.Visibility = Visibility.Collapsed;
            }

            // If this is the logged in user's comment, show edit button instead of like button
            if (comment.User.ID == App.Client.MyUserID)
            {
                likeButton.Visibility = Visibility.Collapsed;
                editButton.Visibility = Visibility.Visible;
            }

            // Enable or disable reply button as needed
            replyButton.IsEnabled = comment.User.CanReplyToComments();
        }
    }
}
