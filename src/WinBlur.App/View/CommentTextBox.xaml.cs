using Microsoft.UI.Xaml.Controls;
using WinBlur.App.Model;

namespace WinBlur.App.View
{
    public sealed partial class CommentTextBox : UserControl
    {
        public Button DeleteButton { get { return deleteButton; } }
        public Button CancelButton { get { return cancelButton; } }
        public Button SubmitButton { get { return submitButton; } }
        public TextBox CommentsBox { get { return commentTextBox; } }
        public ProgressRing SubmitProgress { get { return submitProgress; } }

        // If this text box is replying to an original comment, these will be set.
        public Comment SourceComment { get; set; }
        public CommentBubble SourceCommentBubble { get; set; }

        public CommentTextBox()
        {
            InitializeComponent();

            deleteButton.Tag = this;
            cancelButton.Tag = this;
            submitButton.Tag = this;
        }
    }
}
