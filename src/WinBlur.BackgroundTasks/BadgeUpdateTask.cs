using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using WinBlur.Shared;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace WinBlur.BackgroundTasks
{
    public sealed class BadgeUpdateTask : IBackgroundTask
    {
        private HttpClient m_httpClient;
        private HttpBaseProtocolFilter m_httpFilter;
        private Uri m_baseUri;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Create variables
            m_httpFilter = new HttpBaseProtocolFilter();
            m_httpClient = new HttpClient(m_httpFilter);
            m_httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json; q=0.01");
            m_httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            m_httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
            m_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko");
            m_baseUri = new Uri("https://www.newsblur.com");

            // Even though we aren't setting live tiles anymore, clear any tiles just to make sure they aren't stale.
            TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.Clear();

            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            uint unreadCount = 0;
            try
            {
                if (IsLoggedIn())
                {
                    unreadCount = await GetUnreadCountAsync();
                }
            }
            catch (Exception)
            {
            }

            BadgeHelper.UpdateNumericBadge(unreadCount);

            deferral.Complete();
        }

        private bool IsLoggedIn()
        {
            bool isLoggedIn = false;
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
                        isLoggedIn = true;
                        break;
                    }
                    else
                    {
                        // Delete invalid cookies
                        m_httpFilter.CookieManager.DeleteCookie(cookie);
                    }
                }
            }
            return isLoggedIn;
        }

        private bool IsValidCookie(HttpCookie cookie)
        {
            return !cookie.Expires.HasValue ||
                   (cookie.Expires.Value.CompareTo(DateTimeOffset.UtcNow) > 0);
        }

        private async Task<uint> GetUnreadCountAsync()
        {
            Uri requestUri = new Uri(m_baseUri, "/reader/refresh_feeds");
            string response = await m_httpClient.GetStringAsync(requestUri);
            JObject pResponse = JObject.Parse(response);

            uint unreadCount = 0;
            if ((bool)pResponse["authenticated"])
            {
                if (pResponse["feeds"] is JObject counts)
                {
                    foreach (JToken t in counts.Children())
                    {
                        if (((JProperty)t).Value is JObject countToken)
                        {
                            int id = ParseHelper.ParseValueStruct(countToken["id"], -1);
                            if (id != -1)
                            {
                                uint psCount = ParseHelper.ParseValueStruct(countToken["ps"], 0u);
                                uint ntCount = ParseHelper.ParseValueStruct(countToken["nt"], 0u);
                                unreadCount += psCount + ntCount;
                            }
                        }
                    }
                }
            }
            return unreadCount;
        }
    }
}
