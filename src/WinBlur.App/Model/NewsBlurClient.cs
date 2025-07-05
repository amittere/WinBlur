using Microsoft.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinBlur.App.ViewModel;
using WinBlur.Shared;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace WinBlur.App.Model
{
    public class NewsBlurClient
    {
        #region Properties

        private HttpClient m_httpClient;
        private HttpBaseProtocolFilter m_httpFilter;
        private Uri m_baseUri;

        public int MyUserID { get; set; }
        public User LoggedInUser { get { return Users[MyUserID]; } }
        public Dictionary<int, Feed> Feeds { get; set; }
        public Dictionary<int, SocialFeed> Friends { get; set; }
        public Dictionary<int, User> Users { get; set; }
        public Dictionary<string, int> SavedStoryTags { get; set; }
        public int SavedStoryCount { get; set; }
        public ReadArticleQueue ReadQueue { get; private set; }

        #endregion Properties

        #region Events

        public event EventHandler UnreadCountsChanged;
        public void NotifyUnreadCountsChanged()
        {
            UnreadCountsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler FeedMarkedAsRead;
        public void NotifyFeedMarkedAsRead()
        {
            FeedMarkedAsRead?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Initialization

        public NewsBlurClient()
        {
            m_httpFilter = new HttpBaseProtocolFilter();
            m_httpClient = new HttpClient(m_httpFilter);
            m_httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json; q=0.01");
            m_httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            m_httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
            m_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko");
            m_baseUri = new Uri("https://www.newsblur.com");

            Feeds = new Dictionary<int, Feed>();
            Friends = new Dictionary<int, SocialFeed>();
            Users = new Dictionary<int, User>();
            SavedStoryTags = new Dictionary<string, int>();
            ReadQueue = new ReadArticleQueue();
        }

        #endregion

        #region Login

        public async Task<string> Signup(string username, string password, string email)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("email", email)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/api/signup");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> Signup(string username, string password, string email, CancellationToken token)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("email", email)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/api/signup");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> Login(string username, string password)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/api/login");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> Logout()
        {
            HttpStringContent requestData = new HttpStringContent("");

            Uri requestUri = new Uri(m_baseUri, "/api/logout");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        #endregion Login

        #region Feeds

        public async Task<string> GetSiteList(
            bool includeFavicons = false,
            bool flat = false,
            bool updateCounts = false)
        {
            Uri requestUri = new Uri(m_baseUri, 
                string.Format("/reader/feeds?include_favicons={0}&updateCounts={1}", 
                includeFavicons, updateCounts));
            return await SendGetRequestAsync(requestUri);
        }

        public async Task<string> GetUnreadCounts()
        {
            Uri requestUri = new Uri(m_baseUri, "/reader/refresh_feeds");
            return await SendGetRequestAsync(requestUri);
        }

        public async Task<string> GetSingleSiteStoriesByPage(
            int id, 
            int page = 1,
            bool unreadOnly = true, 
            bool newestFirst = true, 
            bool includeHidden = false,
            bool includeStoryContent = true)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            Uri requestUri = new Uri(m_baseUri, 
                string.Format("/reader/feed/{0}?page={1}&read_filter={2}&order={3}", 
                id, page, filter, order));
            return await SendGetRequestAsync(requestUri);
        }

        public async Task<string> GetSingleSiteStoriesByPage(
            CancellationToken token,
            int id,
            int page = 1,
            bool unreadOnly = true,
            bool newestFirst = true,
            bool includeHidden = false,
            bool includeStoryContent = true)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            Uri requestUri = new Uri(m_baseUri,
                string.Format("/reader/feed/{0}?page={1}&read_filter={2}&order={3}",
                id, page, filter, order));
            return await SendGetRequestAsync(requestUri, token);
        }

        public async Task<string> GetMultiSiteStoriesByPage(
            IEnumerable<int> ids, 
            int page = 1, 
            bool unreadOnly = true, 
            bool newestFirst = true,
            bool includeHidden = false)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("/reader/river_stories?read_filter={0}&order={1}&page={2}&include_hidden={3}", 
                filter, order, page, includeHidden);
            foreach (int id in ids)
            {
                sb.AppendFormat("&feeds={0}", id);
            }

            Uri requestUri = new Uri(m_baseUri, sb.ToString());
            return await SendGetRequestAsync(requestUri);
        }

        public async Task<string> GetMultiSiteStoriesByPage(
            CancellationToken token,
            IEnumerable<int> ids,
            int page = 1,
            bool unreadOnly = true,
            bool newestFirst = true,
            bool includeHidden = false)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("/reader/river_stories?read_filter={0}&order={1}&page={2}&include_hidden={3}",
                filter, order, page, includeHidden);
            foreach (int id in ids)
            {
                sb.AppendFormat("&feeds={0}", id);
            }

            Uri requestUri = new Uri(m_baseUri, sb.ToString());
            return await SendGetRequestAsync(requestUri, token);
        }

        public async Task<string> GetSavedStoriesByPage(int page = 1, string tag = "")
        {
            return await GetSavedStoriesByPage(CancellationToken.None, page, tag);
        }

        public async Task<string> GetSavedStoriesByPage(CancellationToken token, int page = 1, string tag = "")
        {
            Uri requestUri = new Uri(m_baseUri, string.Format("/reader/starred_stories?page={0}&tag={1}", page, tag));
            return await SendGetRequestAsync(requestUri, token);
        }

        #endregion Feeds

        #region Stories

        public async Task<string> GetOriginalStoryText(string storyID, int feedID)
        {
            return await GetOriginalStoryText(CancellationToken.None, storyID, feedID);
        }

        public async Task<string> GetOriginalStoryText(CancellationToken token, string storyID, int feedID)
        {
            Uri requestUri = new Uri(m_baseUri, string.Format("/rss_feeds/original_text?story_id={0}&feed_id={1}", storyID, feedID));
            string response = await SendGetRequestAsync(requestUri, token);

            // Parse response, just return the original text to the caller
            JObject pResponse = JObject.Parse(response);
            if ((bool)pResponse["authenticated"])
            {
                return ParseHelper.ParseValueRef<string>(pResponse["original_text"], "");
            }

            return null;
        }

        #endregion

        #region Story Management

        public async Task<string> MarkStoriesAsRead(IEnumerable<string> hashes)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            foreach (string hash in hashes)
            {
                data.Add(new KeyValuePair<string, string>("story_hash", hash));
            }
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_story_hashes_as_read");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> MarkFeedsAsRead(IEnumerable<int> feedIDs, long timestampCutoff = 0, bool directionOlder = false)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();

            foreach (int id in feedIDs)
            {
                data.Add(new KeyValuePair<string, string>("feed_id", id.ToString()));
            }

            if (timestampCutoff != 0)
            {
                data.Add(new KeyValuePair<string, string>("cutoff_timestamp", timestampCutoff.ToString()));
                data.Add(new KeyValuePair<string, string>("direction", directionOlder ? "older" : "newer"));
            }

            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_feed_as_read");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> MarkAllAsRead(int days = 0)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("days", days.ToString())
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_all_as_read");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> MarkStoryAsUnread(string hash)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_hash", hash)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_story_hash_as_unread");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> MarkStoryAsStarred(string hash)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_hash", hash)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_story_hash_as_starred");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> MarkStoryAsUnstarred(string hash)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_hash", hash)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/mark_story_hash_as_unstarred");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        #endregion Story Management

        #region Feed Management

        public async Task<string> AutoCompleteSite(string query)
        {
            return await AutoCompleteSite(CancellationToken.None, query);
        }

        public async Task<string> AutoCompleteSite(CancellationToken token, string query)
        {
            Uri requestUri = new Uri(m_baseUri, 
                string.Format("/rss_feeds/feed_autocomplete?term={0}", query));
            return await SendGetRequestAsync(requestUri, token);
        }

        public async Task<string> AddSite(string url, string folder = "")
        {
            return await AddSite(CancellationToken.None, url, folder);
        }

        public async Task<string> AddSite(CancellationToken token, string url, string folder = "")
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("url", url),
                new KeyValuePair<string, string>("folder", folder)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/add_url");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> RemoveSite(int id, string folder = "")
        {
            return await RemoveSite(CancellationToken.None, id, folder);
        }

        public async Task<string> RemoveSite(CancellationToken token, int id, string folder = "")
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("feed_id", id.ToString()),
                new KeyValuePair<string, string>("in_folder", folder)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/delete_feed");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> AddFolder(string folder, string parent = "")
        {
            return await AddFolder(CancellationToken.None, folder, parent);
        }

        public async Task<string> AddFolder(CancellationToken token, string folder, string parent = "")
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("folder", folder),
                new KeyValuePair<string, string>("parent_folder", parent)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/add_folder");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> RemoveFolder(string folder, string parent = "", IEnumerable<int> feedIDs = null)
        {
            return await RemoveFolder(CancellationToken.None, folder, parent, feedIDs);
        }

        public async Task<string> RemoveFolder(CancellationToken token, string folder, string parent = "", IEnumerable<int> feedIDs = null)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("folder_name", folder),
                new KeyValuePair<string, string>("in_folder", parent)
            };
            if (feedIDs != null)
            {
                foreach (int id in feedIDs)
                {
                    data.Add(new KeyValuePair<string, string>("feed_id", id.ToString()));
                }
            }
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/delete_folder");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> MoveSite(int id, string fromFolder, string toFolder)
        {
            return await MoveSite(CancellationToken.None, id, fromFolder, toFolder);
        }

        public async Task<string> MoveSite(CancellationToken token, int id, string fromFolder, string toFolder)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("feed_id", id.ToString()),
                new KeyValuePair<string, string>("in_folder", fromFolder),
                new KeyValuePair<string, string>("to_folder", toFolder)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/move_feed_to_folder");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> MoveFolder(string folder, string fromFolder, string toFolder)
        {
            return await MoveFolder(CancellationToken.None, folder, fromFolder, toFolder);
        }

        public async Task<string> MoveFolder(CancellationToken token, string folder, string fromFolder, string toFolder)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("folder_name", folder),
                new KeyValuePair<string, string>("in_folder", fromFolder),
                new KeyValuePair<string, string>("to_folder", toFolder)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/move_folder_to_folder");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> RenameSite(int id, string name)
        {
            return await RenameSite(CancellationToken.None, id, name);
        }

        public async Task<string> RenameSite(CancellationToken token, int id, string name)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("feed_id", id.ToString()),
                new KeyValuePair<string, string>("feed_title", name)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/rename_feed");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        public async Task<string> RenameFolder(string folderName, string newFolderName, string inFolder)
        {
            return await RenameFolder(CancellationToken.None, folderName, newFolderName, inFolder);
        }

        public async Task<string> RenameFolder(CancellationToken token, string folderName, string newFolderName, string inFolder)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("folder_name", folderName),
                new KeyValuePair<string, string>("new_folder_name", newFolderName),
                new KeyValuePair<string, string>("in_folder", inFolder)
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/reader/rename_folder");
            return await SendPostRequestAsync(requestUri, requestData, token);
        }

        #endregion Feed Management

        #region Social

        public async Task<string> GetSingleSocialStoriesByPage(
            int id,
            int page = 1,
            bool unreadOnly = false,
            bool newestFirst = true)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            Uri requestUri = new Uri(m_baseUri,
                string.Format("/social/stories/{0}/?page={1}&read_filter={2}&order={3}",
                id, page, filter, order));
            return await SendGetRequestAsync(requestUri);
        }

        public async Task<string> GetSingleSocialStoriesByPage(
            CancellationToken token,
            int id,
            int page = 1,
            bool unreadOnly = false,
            bool newestFirst = true)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            Uri requestUri = new Uri(m_baseUri,
                string.Format("/social/stories/{0}/?page={1}&read_filter={2}&order={3}",
                id, page, filter, order));
            return await SendGetRequestAsync(requestUri, token);
        }

        public async Task<string> GetMultiSocialStoriesByPage(
            int page = 1,
            bool unreadOnly = false,
            bool newestFirst = true,
            bool globalFeed = false)
        {
            return await GetMultiSocialStoriesByPage(CancellationToken.None, page, unreadOnly, newestFirst, globalFeed);
        }

        public async Task<string> GetMultiSocialStoriesByPage(
            CancellationToken token,
            int page = 1,
            bool unreadOnly = false,
            bool newestFirst = true,
            bool globalFeed = false)
        {
            string filter = unreadOnly ? "unread" : "all";
            string order = newestFirst ? "newest" : "oldest";

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("/social/river_stories?page={0}&read_filter={1}&order={2}",
                page, filter, order);

            if (globalFeed)
            {
                sb.AppendFormat("&global_feed={0}", globalFeed);
            }

            Uri requestUri = new Uri(m_baseUri, sb.ToString());
            return await SendGetRequestAsync(requestUri, token);
        }

        public async Task<string> ShareStory(
            int feedID,
            string storyID,
            string shareComments = null,
            int sourceUserID = -1)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("feed_id", feedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
            };
            if (shareComments != null)
            {
                data.Add(new KeyValuePair<string, string>("comments", shareComments));
            }
            if (sourceUserID != -1)
            {
                data.Add(new KeyValuePair<string, string>("source_user_id", sourceUserID.ToString()));
            }
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/share_story");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> UnshareStory(
            int feedID,
            string storyID)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("feed_id", feedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/unshare_story");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> SaveCommentReply(
            int storyFeedID,
            string storyID,
            int commentUserID,
            string replyComments,
            string replyID = null)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_feed_id", storyFeedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
                new KeyValuePair<string, string>("comment_user_id", commentUserID.ToString()),
                new KeyValuePair<string, string>("reply_comments", replyComments)
            };
            if (replyID != null)
            {
                data.Add(new KeyValuePair<string, string>("reply_id", replyID));
            }
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/save_comment_reply");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> RemoveCommentReply(
            int storyFeedID,
            string storyID,
            int commentUserID,
            string replyID = null)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_feed_id", storyFeedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
                new KeyValuePair<string, string>("comment_user_id", commentUserID.ToString()),
            };
            if (replyID != null)
            {
                data.Add(new KeyValuePair<string, string>("reply_id", replyID));
            }
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/remove_comment_reply");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> LikeComment(
            int storyFeedID,
            string storyID,
            int commentUserID)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_feed_id", storyFeedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
                new KeyValuePair<string, string>("comment_user_id", commentUserID.ToString()),
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/like_comment");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        public async Task<string> RemoveLikeComment(
            int storyFeedID,
            string storyID,
            int commentUserID)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("story_feed_id", storyFeedID.ToString()),
                new KeyValuePair<string, string>("story_id", storyID),
                new KeyValuePair<string, string>("comment_user_id", commentUserID.ToString()),
            };
            HttpFormUrlEncodedContent requestData = new HttpFormUrlEncodedContent(data);

            Uri requestUri = new Uri(m_baseUri, "/social/remove_like_comment");
            return await SendPostRequestAsync(requestUri, requestData);
        }

        #endregion Social

        #region Feed Parsing

        public bool ParseFeeds(string response)
        {
            JObject pResponse = JObject.Parse(response);

            // Parse feed data
            if ((bool)pResponse["authenticated"])
            {
                // Parse user profile
                if (pResponse["social_profile"] is JObject profile)
                {
                    int userID = ParseHelper.ParseValueStruct(profile["user_id"], -1);
                    if (userID != -1 && !Users.ContainsKey(userID))
                    {
                        MyUserID = userID;
                        User u = new User()
                        {
                            ID = userID,
                            Username = ParseHelper.ParseValueRef<string>(profile["username"], null),
                            Location = ParseHelper.ParseValueRef<string>(profile["location"], null),
                            FeedTitle = ParseHelper.ParseValueRef<string>(profile["feed_title"], null),
                            FeedAddressUri = ParseHelper.ParseValueRef<Uri>(profile["feed_address"], null),
                            FeedLinkUri = ParseHelper.ParseValueRef<Uri>(profile["feed_link"], null),
                            NumSubscribers = ParseHelper.ParseValueStruct(profile["num_subscribers"], 0),
                            PhotoUri = ParseHelper.ParseValueRef<Uri>(profile["photo_url"], null),
                            LargePhotoUri = ParseHelper.ParseValueRef<Uri>(profile["large_photo_url"], null),
                            Followers = profile["follower_user_ids"].Select(x => (int)x).ToList(),
                            Following = profile["following_user_ids"].Select(x => (int)x).ToList(),
                            Protected = ParseHelper.ParseValueStruct(profile["protected"], false),
                            Private = ParseHelper.ParseValueStruct(profile["private"], false),
                        };
                        Users.Add(userID, u);
                    }
                }

                // Parse feeds
                if (pResponse["feeds"] is JObject feeds)
                {
                    foreach (JToken t in feeds.Children())
                    {
                        if (((JProperty)t).Value is JObject feedToken)
                        {
                            int id = ParseHelper.ParseValueStruct(feedToken["id"], -1);
                            if (id != -1 && !Feeds.ContainsKey(id))
                            {
                                Feed feed = new Feed
                                {
                                    ID = id,
                                    Title = ParseHelper.ParseValueRef<string>(feedToken["feed_title"], null),
                                    WebsiteURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_link"], null),
                                    FeedURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_address"], null),
                                    Active = ParseHelper.ParseValueStruct(feedToken["active"], true),
                                    PsCount = ParseHelper.ParseValueStruct(feedToken["ps"], 0),
                                    NtCount = ParseHelper.ParseValueStruct(feedToken["nt"], 0),
                                    NgCount = ParseHelper.ParseValueStruct(feedToken["ng"], 0),
                                    FaviconURL = ParseHelper.ParseValueRef<Uri>(feedToken["favicon_url"], null),
                                    FaviconColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_color"], Colors.Black.ToString())),
                                    FaviconFadeColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_fade"], Colors.Black.ToString())),
                                    FaviconBorderColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_border"], Colors.Black.ToString())),
                                };
                                Feeds[id] = feed;
                            }
                        }
                    }
                }

                // Parse social feeds
                if (pResponse["social_feeds"] is JArray socialFeeds)
                {
                    foreach (JToken t in socialFeeds.Children())
                    {
                        if (t is JObject feedToken)
                        {
                            int id = ParseHelper.ParseValueStruct(feedToken["user_id"], -1);
                            if (id != -1 && !Friends.ContainsKey(id))
                            {
                                SocialFeed feed = new SocialFeed()
                                {
                                    ID = id,
                                    Title = ParseHelper.ParseValueRef<string>(feedToken["feed_title"], null),
                                    WebsiteURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_link"], null),
                                    FeedURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_address"], null),
                                    PsCount = ParseHelper.ParseValueStruct(feedToken["ps"], 0),
                                    NtCount = ParseHelper.ParseValueStruct(feedToken["nt"], 0),
                                    NgCount = ParseHelper.ParseValueStruct(feedToken["ng"], 0),
                                    PhotoURL = ParseHelper.ParseValueRef<Uri>(feedToken["photo_url"], null),
                                    Username = ParseHelper.ParseValueRef<string>(feedToken["username"], null),
                                };
                                Friends[id] = feed;
                            }
                        }
                    }
                }

                // Parse saved stories
                SavedStoryCount = ParseHelper.ParseValueStruct(pResponse["starred_count"], 0);
                if (pResponse["starred_counts"] is JArray savedTags)
                {
                    foreach (JToken token in savedTags.Children())
                    {
                        string tag = ParseHelper.ParseValueRef<string>(token["tag"], null);
                        if (tag != null && !SavedStoryTags.ContainsKey(tag))
                        {
                            SavedStoryTags[tag] = ParseHelper.ParseValueStruct(token["count"], 0);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public bool ParseUnreadCounts(string response)
        {
            JObject pResponse = JObject.Parse(response);

            if ((bool)pResponse["authenticated"])
            {
                if (pResponse["feeds"] is JObject counts)
                {
                    foreach (JToken t in counts.Children())
                    {
                        if (((JProperty)t).Value is JObject countToken)
                        {
                            int id = ParseHelper.ParseValueStruct(countToken["id"], -1);
                            if (id != -1 && Feeds.ContainsKey(id))
                            {
                                Feeds[id].PsCount = ParseHelper.ParseValueStruct(countToken["ps"], 0);
                                Feeds[id].NtCount = ParseHelper.ParseValueStruct(countToken["nt"], 0);
                                Feeds[id].NgCount = ParseHelper.ParseValueStruct(countToken["ng"], 0);
                            }
                        }
                    }
                }

                if (pResponse["social_feeds"] is JObject socialCounts)
                {
                    foreach (JToken t in socialCounts.Children())
                    {
                        if (((JProperty)t).Value is JObject countToken)
                        {
                            string idString = ParseHelper.ParseValueRef<string>(countToken["id"], null);
                            if (idString != null)
                            {
                                string[] split = idString.Split(':');
                                if (split.Length == 2)
                                {
                                    int id;
                                    bool success = int.TryParse(idString.Split(':')[1], out id);
                                    if (success && Friends.ContainsKey(id))
                                    {
                                        Friends[id].PsCount = ParseHelper.ParseValueStruct(countToken["ps"], 0);
                                        Friends[id].NtCount = ParseHelper.ParseValueStruct(countToken["nt"], 0);
                                        Friends[id].NgCount = ParseHelper.ParseValueStruct(countToken["ng"], 0);
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public Article ParseArticle(JToken t)
        {
            int feedID = ParseHelper.ParseValueStruct(t["story_feed_id"], -1);
            if (feedID != -1 && Feeds.ContainsKey(feedID))
            {
                // First check if the article is already in our list by checking the hash.
                string hash = ParseHelper.ParseValueRef<string>(t["story_hash"], null);
                string title = ParseHelper.ParseValueRef(t["story_title"], "");
                string author = ParseHelper.ParseValueRef<string>(t["story_authors"], null);
                string subtitle;
                if (author == "")
                {
                    subtitle = Feeds[feedID].Title;
                }
                else
                {
                    subtitle = Feeds[feedID].Title + " - " + author;
                }
                string date = ParseHelper.ParseValueRef<string>(t["long_parsed_date"], null);
                Uri articleLink = ParseHelper.ParseValueRef<Uri>(t["story_permalink"], null);

                Article a = new Article
                {
                    ID = ParseHelper.ParseValueRef<string>(t["id"], null),
                    Hash = hash,
                    ArticleLink = articleLink,
                    IsRead = ParseHelper.ParseValueStruct(t["read_status"], 0) == 1,
                    IsStarred = ParseHelper.ParseValueStruct(t["starred"], false),
                    Title = WebUtility.HtmlDecode(title).Replace('\n', ' '),
                    Author = WebUtility.HtmlDecode(author),
                    ContentHeader = CreateContentHeader(title, subtitle, date, articleLink),
                    Content = ParseHelper.ParseValueRef(t["story_content"], ""),
                    ViewContent = "",
                    OriginalText = null,
                    Timestamp = ParseHelper.ParseValueStruct<long>(t["story_timestamp"], 0),
                    ShortDate = ParseHelper.ParseValueRef<string>(t["short_parsed_date"], null),
                    LongDate = date,
                    FeedID = feedID,
                    FeedTitle = Feeds[feedID].Title,
                    FaviconColor = Feeds[feedID].FaviconColor,
                    FaviconFadeColor = Feeds[feedID].FaviconFadeColor,
                    Intelligence = 0,
                    IsShared = ParseHelper.ParseValueStruct(t["shared"], false),
                    SharedComments = ParseHelper.ParseValueRef<string>(t["shared_comments"], null),
                    SourceUserID = ParseHelper.ParseValueStruct(t["source_user_id"], -1),
                    FriendUserIDs = new List<int>(),
                    ShareCount = ParseHelper.ParseValueStruct(t["share_count"], 0),
                    FriendComments = new List<Comment>(),
                    FriendShares = new List<Comment>(),
                    PublicComments = new List<Comment>(),
                    PublicShares = new List<int>(),
                };

                if (t["intelligence"] is JObject intel)
                {
                    a.Intelligence = ParseIntelligence(intel);
                }

                // Parse shares
                if (t["friend_shares"] is JArray friendShares)
                {
                    a.FriendShares = ParseComments(friendShares);
                }
                if (t["shared_by_public"] is JArray publicShares)
                {
                    a.PublicShares = publicShares.Select(id => ParseHelper.ParseValueStruct(id, -1)).ToList();
                }

                // Parse comments
                if (t["friend_comments"] is JArray friendComments)
                {
                    a.FriendComments = ParseComments(friendComments);
                }
                if (t["public_comments"] is JArray publicComments)
                {
                    a.PublicComments = ParseComments(publicComments);
                }

                if (t["friend_user_ids"] != null)
                {
                    a.FriendUserIDs = t["friend_user_ids"].Select(id => ParseHelper.ParseValueStruct(id, -1)).ToList();
                }

                // Parse images
                if (t["secure_image_thumbnails"] is JObject imageThumbnails)
                {
                    a.ImageThumbnail = ParseThumbnail(imageThumbnails);
                }

                return a;
            }
            else
            {
                return null;
            }
        }

        public Tuple<List<Article>, bool> ParseArticles(string response)
        {
            JObject pResponse = JObject.Parse(response);
            List<Article> articleList = new List<Article>();
            Dictionary<string, Article> articleMap = new Dictionary<string, Article>();

            if (!(bool)pResponse["authenticated"]) throw new Exception("Not authenticated");

            // First see if we need to parse feeds first
            if (pResponse["feeds"] is JArray feeds)
            {
                ParseUnsubscribedFeeds(feeds);
            }

            // Parse user profiles if necessary
            if (pResponse["user_profiles"] is JArray userProfiles)
            {
                ParseUserProfiles(userProfiles);
            }

            int numHiddenStories = 0;
            if (pResponse["hidden_stories_removed"] != null)
            {
                numHiddenStories = (int)pResponse["hidden_stories_removed"];
            }

            int numArticles = 0;
            if (pResponse["stories"] is JArray articles)
            {
                numArticles = articles.Count;
                foreach (JToken t in articles.Children())
                {
                    Article a = ParseArticle(t);
                    if (a != null && !articleMap.ContainsKey(a.Hash))
                    {
                        articleList.Add(a);
                        articleMap.Add(a.Hash, a);
                    }
                }
            }

            bool hasMoreItems = numArticles != 0 || numHiddenStories != 0;
            return new Tuple<List<Article>, bool>(articleList, hasMoreItems);
        }

        private int ParseIntelligence(JObject intel)
        {
            int feedIntel = ParseHelper.ParseValueStruct(intel["feed"], 0);
            int tagsIntel = ParseHelper.ParseValueStruct(intel["tags"], 0);
            int authorIntel = ParseHelper.ParseValueStruct(intel["author"], 0);
            int titleIntel = ParseHelper.ParseValueStruct(intel["title"], 0);
            if (feedIntel == 1 || tagsIntel == 1 || authorIntel == 1 || titleIntel == 1)
            {
                return 1;
            }
            else if (feedIntel == -1 || tagsIntel == -1 || authorIntel == -1 || titleIntel == -1)
            {
                return -1;
            }
            return 0;
        }

        public Comment ParseComment(JToken comment)
        {
            Comment result = null;

            int id = ParseHelper.ParseValueStruct(comment["user_id"], -1);
            if (id != -1 && Users.ContainsKey(id))
            {
                // Parse replies to this comment
                ObservableCollection<Comment> replyList = new ObservableCollection<Comment>();
                if (comment["replies"] is JArray replies)
                {
                    foreach (JToken reply in replies.Children())
                    {
                        int replyID = ParseHelper.ParseValueStruct(reply["user_id"], -1);
                        if (replyID != -1 && Users.ContainsKey(replyID))
                        {
                            string relativeDate = ParseHelper.ParseValueRef<string>(reply["publish_date"], null);
                            if (relativeDate != null) relativeDate += " ago";
                            Comment r = new Comment()
                            {
                                ID = ParseHelper.ParseValueRef<string>(reply["reply_id"], null),
                                User = Users[replyID],
                                CommentString = WebUtility.HtmlDecode(ParseHelper.ParseValueRef<string>(reply["comments"], null)),
                                RelativeDate = relativeDate,
                                Likes = new ObservableCollection<int>(),
                                Replies = new ObservableCollection<Comment>(),
                            };
                            replyList.Add(r);
                        }
                    }
                }

                // Parse likes
                IEnumerable<int> list = comment["liking_users"].Select(x => ParseHelper.ParseValueStruct(x, -1));
                ObservableCollection<int> likeList = new ObservableCollection<int>(list);

                // Parse origin comment
                string sharedDate = ParseHelper.ParseValueRef<string>(comment["shared_date"], null);
                if (sharedDate != null) sharedDate += " ago";
                result = new Comment()
                {
                    ID = ParseHelper.ParseValueRef<string>(comment["id"], null),
                    User = Users[id],
                    CommentString = WebUtility.HtmlDecode(ParseHelper.ParseValueRef<string>(comment["comments"], null)),
                    RelativeDate = sharedDate,
                    Likes = likeList,
                    Replies = replyList,
                };
            }

            return result;
        }

        private List<Comment> ParseComments(JArray comments)
        {
            List<Comment> result = new List<Comment>();
            foreach (JToken comment in comments.Children())
            {
                result.Add(ParseComment(comment));
            }
            return result;
        }

        private Uri ParseThumbnail(JObject images)
        {
            foreach (JToken image in images.Children())
            {
                Uri value = ParseHelper.ParseValueRef<Uri>(((JProperty)image).Value, null);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        private void ParseUnsubscribedFeeds(JArray feeds)
        {
            foreach (JToken t in feeds.Children())
            {
                if (t is JObject feedToken)
                {
                    int id = ParseHelper.ParseValueStruct(feedToken["id"], -1);
                    if (id != -1 && !Feeds.ContainsKey(id))
                    {
                        Feed feed = new Feed
                        {
                            ID = id,
                            Title = ParseHelper.ParseValueRef<string>(feedToken["feed_title"], null),
                            WebsiteURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_link"], null),
                            FeedURL = ParseHelper.ParseValueRef<Uri>(feedToken["feed_address"], null),
                            PsCount = ParseHelper.ParseValueStruct(feedToken["ps"], 0),
                            NtCount = ParseHelper.ParseValueStruct(feedToken["nt"], 0),
                            NgCount = ParseHelper.ParseValueStruct(feedToken["ng"], 0),
                            FaviconURL = ParseHelper.ParseValueRef<Uri>(feedToken["favicon_url"], null),
                            FaviconColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_color"], Colors.Black.ToString())),
                            FaviconFadeColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_fade"], Colors.Black.ToString())),
                            FaviconBorderColor = ConvertHexStringToRGB(ParseHelper.ParseValueRef(feedToken["favicon_border"], Colors.Black.ToString())),
                        };
                        Feeds[(int)feedToken["id"]] = feed;
                    }
                }
            }
        }

        private void ParseUserProfiles(JArray users)
        {
            foreach (JToken t in users.Children())
            {
                int id = ParseHelper.ParseValueStruct(t["user_id"], -1);
                if (id != -1 && !Users.ContainsKey(id))
                {
                    User u = new User()
                    {
                        ID = id,
                        Username = ParseHelper.ParseValueRef<string>(t["username"], null),
                        Location = ParseHelper.ParseValueRef<string>(t["location"], null),
                        FeedTitle = ParseHelper.ParseValueRef<string>(t["feed_title"], null),
                        FeedAddressUri = ParseHelper.ParseValueRef<Uri>(t["feed_address"], null),
                        FeedLinkUri = ParseHelper.ParseValueRef<Uri>(t["feed_link"], null),
                        NumSubscribers = ParseHelper.ParseValueStruct(t["num_subscribers"], 0),
                        PhotoUri = ParseHelper.ParseValueRef<Uri>(t["photo_url"], null),
                        LargePhotoUri = ParseHelper.ParseValueRef<Uri>(t["large_photo_url"], null),
                        Protected = ParseHelper.ParseValueStruct(t["protected"], false),
                        Private = ParseHelper.ParseValueStruct(t["private"], false),
                    };
                    Users.Add(id, u);
                }
            }
        }

        public void UpdateReadState(Article article)
        {
            if (!article.IsRead)
            {
                article.IsRead = true;
                Feed f;

                // If its a social feed, then mark it in there
                if (article.FriendUserIDs != null)
                {
                    foreach (int id in article.FriendUserIDs)
                    {
                        if (Friends.ContainsKey(id))
                        {
                            f = Friends[id];
                            switch (article.Intelligence)
                            {
                                case -1:
                                    f.NgCount--;
                                    break;
                                case 0:
                                    f.NtCount--;
                                    break;
                                case 1:
                                    f.PsCount--;
                                    break;
                            }
                        }
                    }
                }

                if (Feeds.ContainsKey(article.FeedID))
                {
                    f = Feeds[article.FeedID];
                    switch (article.Intelligence)
                    {
                        case -1:
                            f.NgCount--;
                            break;
                        case 0:
                            f.NtCount--;
                            break;
                        case 1:
                            f.PsCount--;
                            break;
                    }
                }
            }
        }

        public void UpdateUnreadState(Article article)
        {
            if (article.IsRead)
            {
                article.IsRead = false;
                Feed f;

                // If its a social feed, then mark it in there
                if (article.FriendUserIDs != null)
                {
                    foreach (int id in article.FriendUserIDs)
                    {
                        if (Friends.ContainsKey(id))
                        {
                            f = Friends[id];
                            switch (article.Intelligence)
                            {
                                case -1:
                                    f.NgCount++;
                                    break;
                                case 0:
                                    f.NtCount++;
                                    break;
                                case 1:
                                    f.PsCount++;
                                    break;
                            }
                        }
                    }
                }

                if (Feeds.ContainsKey(article.FeedID))
                {
                    f = Feeds[article.FeedID];
                    switch (article.Intelligence)
                    {
                        case -1:
                            f.NgCount++;
                            break;
                        case 0:
                            f.NtCount++;
                            break;
                        case 1:
                            f.PsCount++;
                            break;
                    }
                }
            }
        }

        #endregion Feed Parsing

        #region Helpers

        private async Task<string> SendGetRequestAsync(Uri requestUri)
        {
            return await SendGetRequestAsync(requestUri, CancellationToken.None);
        }

        private async Task<string> SendGetRequestAsync(Uri requestUri, CancellationToken token)
        {
            bool IsLoggedIn = false;

            // If the http filter has no cookies
            HttpCookieCollection cookies = m_httpFilter.CookieManager.GetCookies(m_baseUri);
            if (cookies.Count > 0)
            {
                foreach (HttpCookie cookie in cookies)
                {
                    if (cookie.Name != "newsblur_sessionid")
                    {
                        // Skip other cookies
                        continue;
                    }

                    if (IsValidCookie(cookie))
                    {
                        // We already have a cookie - set login flag
                        IsLoggedIn = true;
                        break;
                    }
                    else
                    {
                        // Delete invalid cookies
                        m_httpFilter.CookieManager.DeleteCookie(cookie);
                    }
                }
            }

            if (!IsLoggedIn)
            {
                // We have no valid cookie. Check to see if we have credentials saved
                // If so, attempt to login
                PasswordCredential cred = App.Settings.RetrieveLoginCredentials();
                if (cred != null)
                {
                    cred.RetrievePassword();
                    string s = await App.Client.Login(cred.UserName, cred.Password);
                    JObject json = JObject.Parse(s);
                    if ((int)json["code"] == 1 && (bool)json["authenticated"] == true)
                    {
                        IsLoggedIn = true;
                    }
                }
                else if (App.Settings.LoginUsername != "")
                {
                    string s = await App.Client.Login(App.Settings.LoginUsername, "");
                    JObject json = JObject.Parse(s);
                    if ((int)json["code"] == 1 && (bool)json["authenticated"] == true)
                    {
                        IsLoggedIn = true;
                    }
                }
            }

            if (!IsLoggedIn)
            {
                // Logging in failed - clear login creds and crash so next launch forces a new login.
                App.Settings.ClearLoginCredentials();
                throw new Exception("Not logged in!");
            }

            HttpResponseMessage response = await m_httpClient.GetAsync(requestUri).AsTask(token);
            token.ThrowIfCancellationRequested();

            response.EnsureSuccessStatusCode();

            string str = await response.Content.ReadAsStringAsync().AsTask(token);
            token.ThrowIfCancellationRequested();

            return str;
        }

        private async Task<string> SendPostRequestAsync(Uri requestUri, IHttpContent requestData)
        {
            return await SendPostRequestAsync(requestUri, requestData, CancellationToken.None);
        }

        private async Task<string> SendPostRequestAsync(Uri requestUri, IHttpContent requestData, CancellationToken token)
        {
            HttpResponseMessage response = await m_httpClient.PostAsync(requestUri, requestData).AsTask(token);
            token.ThrowIfCancellationRequested();

            response.EnsureSuccessStatusCode();

            string str = await response.Content.ReadAsStringAsync().AsTask(token);
            token.ThrowIfCancellationRequested();

            return str;
        }

        public void DeleteCookies()
        {
            HttpCookieCollection cookies = m_httpFilter.CookieManager.GetCookies(m_baseUri);
            foreach (HttpCookie cookie in cookies)
            {
                m_httpFilter.CookieManager.DeleteCookie(cookie);
            }
        }

        private bool IsValidCookie(HttpCookie cookie)
        {
            return !cookie.Expires.HasValue ||
                   (cookie.Expires.Value.CompareTo(DateTimeOffset.UtcNow) > 0);
        }

        public Color ConvertHexStringToRGB(string hexString)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (hexString.StartsWith("#"))
            {
                hexString = hexString.Substring(1);
            }
            if (hexString.Length == 8)
            {
                // This is ARGB color. parse all of them out
                a = Convert.ToByte(int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier));
                r = Convert.ToByte(int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier));
                g = Convert.ToByte(int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier));
                b = Convert.ToByte(int.Parse(hexString.Substring(6, 2), NumberStyles.AllowHexSpecifier));
                return Color.FromArgb(a, r, g, b);

            }
            else
            {
                // Regular RGB
                r = Convert.ToByte(int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier));
                g = Convert.ToByte(int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier));
                b = Convert.ToByte(int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier));
                return Color.FromArgb(255, r, g, b);
            }
        }

        private string CreateContentHeader(string title, string subtitle, string date, Uri articleLink)
        {
            return string.Format(@"
                <div class=""winblur-title""><a class=""winblur-title-link"" href={0}>{1}</a></div>
                <div class=""winblur-caption"">{2}<br>{3}</div>",
                articleLink,
                title,
                subtitle,
                date);
        }

        #endregion Helpers
    }
}
