using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WinBlur.App.Model
{
    public class Comment : INotifyPropertyChanged
    {
        public string ID { get; set; }
        public User User { get; set; }
        public string RelativeDate { get; set; }
        public string CommentString { get; set; }
        public ObservableCollection<int> Likes { get; set; }
        public ObservableCollection<Comment> Replies { get; set; }

        public bool IsLiked
        {
            get
            {
                return Likes.Contains(App.Client.MyUserID);
            }
            set
            {
                if (value && !IsLiked)
                {
                    Likes.Add(App.Client.MyUserID);
                    NotifyPropertyChanged("IsLiked");
                }
                else if (!value && IsLiked)
                {
                    Likes.Remove(App.Client.MyUserID);
                    NotifyPropertyChanged("IsLiked");
                }
            }
        }

        public Comment() { }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}