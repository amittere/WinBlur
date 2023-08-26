using System;
using System.Collections.Generic;

namespace WinBlur.App.Model
{
    public class User
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Location { get; set; }
        public Uri FeedAddressUri { get; set; }
        public Uri FeedLinkUri { get; set; }
        public string FeedTitle { get; set; }
        public int NumSubscribers { get; set; }
        public Uri LargePhotoUri { get; set; }
        public Uri PhotoUri { get; set; }
        public List<int> Followers { get; set; }
        public List<int> Following { get; set; }
        public bool Protected { get; set; }
        public bool Private { get; set; }

        public User() { }

        public bool CanReplyToComments()
        {
            if (this.Protected)
                return App.Client.Users[App.Client.MyUserID].Following.Contains(this.ID);
            return true;
        }
    }
}