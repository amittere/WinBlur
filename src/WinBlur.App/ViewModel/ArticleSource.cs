using CommunityToolkit.Common.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinBlur.App.Model;

namespace WinBlur.App.ViewModel
{
    public class ArticleSource : IIncrementalSource<Article>
    {
        int currentPage = 1;
        bool hasMoreItems = true;
        List<Article> articles = new List<Article>();

        public SubscriptionType Type { get; set; }
        public bool IsFolder { get; set; }
        public bool UnreadOnly { get; set; }
        public bool NewestFirst { get; set; }
        public List<int> Feeds { get; set; }
        public string Tag { get; set; }

        public async Task<IEnumerable<Article>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken token = default)
        {
            if (pageIndex == 0)
            {
                // First load, reset state. This ensures that calls to RefreshAsync() actually refresh the source.
                currentPage = 1;
                hasMoreItems = true;
                articles.Clear();
            }

            // Load more items until we have enough to satisfy the request, or we run out of items.
            while (hasMoreItems && articles.Count < (pageIndex * pageSize) + pageSize)
            {
                await LoadMoreItemsAsync(token);
            }

            return articles.Skip(pageIndex * pageSize).Take(pageSize);
        }

        private async Task LoadMoreItemsAsync(CancellationToken token)
        {
            string response = null;
            switch (Type)
            {
                case SubscriptionType.Global:
                    response = await App.Client.GetMultiSocialStoriesByPage(
                        token, currentPage, UnreadOnly, NewestFirst, true);
                    break;

                case SubscriptionType.Social:
                    if (IsFolder)
                    {
                        response = await App.Client.GetMultiSocialStoriesByPage(
                            token, currentPage, UnreadOnly, NewestFirst);
                    }
                    else
                    {
                        response = await App.Client.GetSingleSocialStoriesByPage(
                            token, Feeds[0], currentPage, UnreadOnly, NewestFirst);
                    }
                    break;

                case SubscriptionType.Site:
                    if (IsFolder)
                    {
                        response = await App.Client.GetMultiSiteStoriesByPage(
                            token, Feeds, currentPage, UnreadOnly, NewestFirst);
                    }
                    else
                    {
                        response = await App.Client.GetSingleSiteStoriesByPage(
                            token, Feeds[0], currentPage, UnreadOnly, NewestFirst);
                    }
                    break;

                case SubscriptionType.Saved:
                    if (IsFolder)
                    {
                        response = await App.Client.GetSavedStoriesByPage(token, currentPage);
                    }
                    else
                    {
                        response = await App.Client.GetSavedStoriesByPage(token, currentPage, Tag);
                    }
                    break;
            }
            if (response == null) throw new Exception("Empty response");

            Tuple<List<Article>, bool> result = App.Client.ParseArticles(response);

            currentPage++;
            hasMoreItems = result.Item2;
            articles.AddRange(result.Item1);
        }
    }
}
