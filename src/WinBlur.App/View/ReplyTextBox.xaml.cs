using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinBlur.App.Model;

namespace WinBlur.App.View
{
    public partial class ReplyTextBox : UserControl
    {
        public Button DeleteButton { get { return deleteButton; } }
        public Button CancelButton { get { return cancelButton; } }
        public Button SubmitButton { get { return submitButton; } }
        public TextBox ReplyContentsBox { get { return commentTextBox; } }
        public ProgressRing SubmitProgress { get { return submitProgress; } }

        // If this text box is replying to an original comment, these will be set.
        public Comment SourceComment { get; set; }
        public CommentBubble SourceCommentBubble { get; set; }

        // If this text box is editing an existing reply, these will also be set in addition to the above.
        private Comment m_originalReply;
        public Comment OriginalReply
        {
            get
            {
                return m_originalReply;
            }
            set
            {
                if (value != null)
                {
                    // Since this is editing an existing reply,
                    // Update UI to match this
                    SubmitButton.Content = "Save";
                    deleteButton.Visibility = Visibility.Visible;
                }
                else
                {
                    SubmitButton.Content = "Post";
                    deleteButton.Visibility = Visibility.Collapsed;
                }
                m_originalReply = value;
            }
        }

        public ReplyTextBox()
        {
            InitializeComponent();

            deleteButton.Tag = this;
            cancelButton.Tag = this;
            submitButton.Tag = this;
        }
    }
}
