using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WinBlur.App.Model
{
    public class ReadArticleQueue
    {
        private Timer _timer;
        private List<string> _queue;
        private AutoResetEvent _lock;
        private const int DELAY = 10000;

        public ReadArticleQueue()
        {
            _queue = new List<string>();
            _lock = new AutoResetEvent(true);
            _timer = new Timer(ProcessQueue, null, DELAY, DELAY);
        }

        public async void AddArticleToQueue(Article a)
        {
            await Task.Run(() =>
            {
                _lock.WaitOne();

                if (!_queue.Contains(a.Hash))
                {
                    _queue.Add(a.Hash);
                }

                _lock.Set();
            });
        }

        public async void RemoveArticleFromQueue(Article a)
        {
            await Task.Run(() =>
            {
                _lock.WaitOne();

                _queue.Remove(a.Hash);

                _lock.Set();
            });
        }

        private async void ProcessQueue(object state)
        {
            await Task.Run(async () =>
            {
                _lock.WaitOne();

                if (_queue.Count > 0)
                {
                    try
                    {
                        List<string> queueTemp = new List<string>(_queue);
                        await App.Client.MarkStoriesAsRead(queueTemp);
                        _queue.Clear();
                    }
                    catch (Exception)
                    {
                        // TODO: log some telemetry
                    }
                }

                _lock.Set();
            });
        }
    }
}
